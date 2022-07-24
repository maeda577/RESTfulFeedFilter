using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace Rff;

internal class Program
{
    private static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.All;
        });

        // 標準の出力を切ってXMLに限定する https://docs.microsoft.com/ja-jp/aspnet/core/web-api/advanced/formatting?view=aspnetcore-6.0
        builder.Services.AddControllers(options =>
        {
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
            options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();
        }).AddXmlSerializerFormatters();

        // XML出力でXML Declarationが消えないようにする
        builder.Services.Configure<MvcOptions>(options =>
        {
            XmlSerializerOutputFormatter? xmlWriterSettings = options.OutputFormatters
                .OfType<XmlSerializerOutputFormatter>()
                .FirstOrDefault();
            if (xmlWriterSettings != null)
            {
                xmlWriterSettings.WriterSettings.OmitXmlDeclaration = false;
                xmlWriterSettings.WriterSettings.Indent = true;
            }
        });

        WebApplication app = builder.Build();
        app.UseHttpLogging();

        // 静的ファイルの提供 https://docs.microsoft.com/ja-jp/aspnet/core/fundamentals/static-files?view=aspnetcore-6.0
        app.UseFileServer();

        // 属性ルーティングの有効化 https://docs.microsoft.com/ja-jp/aspnet/core/mvc/controllers/routing?view=aspnetcore-6.0#attribute-routing-for-rest-apis
        // APIでは規則ルーティングを使わないらしい
        app.MapControllers();

        app.Run();
    }
}
