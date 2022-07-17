using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Xml.XPath;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Rff.Controllers;

/// <summary>FeedFilterのコントローラ</summary>
[ApiController]
[Route("api/[controller]")]
public class FeedController : ControllerBase
{
    private static readonly IReadOnlySet<string> URL_SCHEMES = new HashSet<string>() { Uri.UriSchemeHttp, Uri.UriSchemeHttps };

    // APIの戻り値はActionResult https://docs.microsoft.com/ja-jp/aspnet/core/web-api/action-return-types?view=aspnetcore-6.0
    /// <summary>Filter items from feed.</summary>
    /// <remarks>
    /// Note:
    /// <p>
    /// Namespaces defined in the original feed are also available in XPath.<br />
    /// Default namespace in the original feed is available as "default" prefix.<br />
    /// </p>
    /// Example XPath for atom feed: /default:feed/default:entry[starts-with(default:title, 'PR')]
    /// </remarks>
    /// <param name="feedUrl" example="https://example.com/feed.rss">Source feed URL.</param>
    /// <param name="xpath" example="/rss/channel/item[starts-with(title, 'PR')]">XPath that matches the item you want to exclude.</param>
    /// <response code="200">OK</response>
    /// <response code="400">Invalid Parameters</response>
    /// <response code="500">Internal Error</response>
    [HttpGet("filter")]
    [Produces("application/xml")]
    public ActionResult<XmlDocument> FilterFeed(
        [FromQuery(Name = "feedUrl")]
        [CustomValidation(typeof(FeedController), nameof(FeedController.ValidateUrl))]  // System.ComponentModel.DataAnnotations.UrlAttribute はなぜか動かなかったので自作した
        [Required]
        Uri feedUrl,

        [FromQuery(Name = "xpath")]
        [CustomValidation(typeof(FeedController), nameof(FeedController.ValidateXPath))]
        [Required]
        string xpath
    )
    {
        XmlDocument xml = new XmlDocument();
        try
        {
            // XML読み込み
            xml.Load(feedUrl.AbsoluteUri);

            // XMLの方に定義されているxmlnsをコピーする.デフォルトのものがあれば適当にdefaultのプレフィックスでコピーする
            XmlNamespaceManager namespaces = new XmlNamespaceManager(xml.NameTable);
            xml.DocumentElement?.Attributes
                .Cast<XmlAttribute>()
                .Where(attr => attr.Prefix == "xmlns" || attr.LocalName == "xmlns")
                .ToList()
                .ForEach(attr => namespaces.AddNamespace(attr.Prefix == "xmlns" ? attr.LocalName : "default", attr.Value));

            // XPathにマッチするノードを消す
            xml.SelectNodes(xpath, namespaces)
                ?.Cast<XmlNode>()
                .ToList()
                .ForEach(node => node.ParentNode?.RemoveChild(node));

            return xml;
        }
        catch (HttpRequestException e)  // XMLを取得しに行ったが失敗した
        {
            return this.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "An error occurred while fetching external feed.",
                detail: e.Message
            );
        }
        catch (XmlException e)          // 取得したものがXMLじゃなかった
        {
            return this.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "An error occurred while parsing XML.",
                detail: e.Message
            );
        }
        catch (XPathException e)        // XPathで未定義のNamespaceが使われていた
        {
            return this.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: $"The {nameof(xpath)} field is not a valid XPath.",
                detail: e.Message
            );
        }
        catch (Exception e)             // その他
        {
            return this.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred.",
                detail: e.Message
            );
        }
    }

    /// <summary>XPathのバリデーション</summary>
    public static ValidationResult ValidateXPath(string xpath, ValidationContext context)
    {
        try
        {
            _ = XPathExpression.Compile(xpath);
            return ValidationResult.Success!;
        }
        catch
        {
            return new ValidationResult($"The {context.MemberName} field is not a valid XPath.");
        }
    }

    /// <summary>URLのバリデーション</summary>
    public static ValidationResult ValidateUrl(Uri url, ValidationContext context)
    {
        if (url != null && url.IsAbsoluteUri && URL_SCHEMES.Contains(url.Scheme))
        {
            return ValidationResult.Success!;
        }
        return new ValidationResult($"The {context.MemberName} field is not a valid URL.");
    }
}
