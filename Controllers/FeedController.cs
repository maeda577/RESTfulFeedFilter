using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text;
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
    /// <p>Example XPath for atom feed: /default:feed/default:entry[starts-with(default:title, 'PR')]</p>
    /// </remarks>
    /// <param name="feedUrl" example="https://example.com/feed.rss">Source feed URL.</param>
    /// <param name="xpath" example="/rss/channel/item[starts-with(title, 'PR')]">XPath that matches the item you want to exclude.</param>
    /// <param name="executePostProcessForRss1">If source feed is RSS1.0, rdf:Seq elements associated with the excluded elements are also excluded.</param>
    /// <response code="200">OK. Returns filtered feed XML.</response>
    /// <response code="400">Invalid Parameters.</response>
    /// <response code="500">Internal Error.</response>
    [HttpGet("filter")]
    [Produces("application/xml")]
    public ActionResult FilterFeed(
        [FromQuery(Name = "feedUrl")]
        [CustomValidation(typeof(FeedController), nameof(FeedController.ValidateUrl))]  // System.ComponentModel.DataAnnotations.UrlAttribute はなぜか動かなかったので自作した
        [Required]
        Uri feedUrl,

        [FromQuery(Name = "xpath")]
        [CustomValidation(typeof(FeedController), nameof(FeedController.ValidateXPath))]
        [Required]
        string xpath,

        [FromQuery(Name = "executePostProcessForRss1")]
        bool executePostProcessForRss1 = true
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

            // XPathにマッチするノードを消しつつ収集する(ルート要素は消せない仕様にした)
            List<XmlNode>? deletedNodes = xml.SelectNodes(xpath, namespaces)
                ?.Cast<XmlNode>()
                .Where(node => node.ParentNode != null)
                .Select(node => node.ParentNode!.RemoveChild(node))
                .ToList();

            // RSS1.0にあるSeqノードをXPath決め打ちで探す
            XmlNode? seqNode = xml.SelectSingleNode("/rdf:RDF/default:channel/default:items/rdf:Seq", namespaces);
            if (executePostProcessForRss1 && deletedNodes != null && seqNode != null)
            {
                // 削除したノードのrdf:aboutを集める
                HashSet<string?> deletedUrls = deletedNodes.Select(node => node.Attributes?["rdf:about"]?.Value).ToHashSet();
                deletedUrls.Remove(null);
                // Seqノードの子要素から削除したノードに紐づく要素を消す
                seqNode.ChildNodes.Cast<XmlNode>()
                    .Where(node => deletedUrls.Contains(node.Attributes?["rdf:resource"]?.Value))
                    .ToList()
                    .ForEach(node => node.ParentNode!.RemoveChild(node));
            }

            // 出力がUTF8決め打ちなので、XmlDeclarationがあればEncodingをUTF8にする
            if (xml.FirstChild?.NodeType == XmlNodeType.XmlDeclaration)
            {
                XmlDeclaration? declaration = xml.FirstChild as XmlDeclaration;
                xml.CreateXmlDeclaration(declaration?.Version ?? "1.0", "UTF-8", declaration?.Standalone);
            }

            // 普通にXmlDocumentを返すと何故かXmlDeclarationが消えるのでFileとして返す
            return this.File(Encoding.UTF8.GetBytes(xml.OuterXml), "application/xml");
        }
        catch (HttpRequestException e)  // XMLを取得しに行ったが失敗した
        {
            return this.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "An error occurred while fetching source feed.",
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
