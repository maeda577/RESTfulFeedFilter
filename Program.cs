using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

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
                // xmlWriterSettings.WriterSettings.NewLineChars = "¥r¥n";
                // xmlWriterSettings.WriterSettings.NewLineHandling = System.Xml.NewLineHandling.Entitize;
            }
        });

        // Swaggerの設定 https://docs.microsoft.com/ja-jp/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-6.0
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "1.0.1",

                Title = "RESTful Feed Filter",
                Description = "This is a simple API to exclude unnecessary elements from RSS, Atom, and other feeds. Unnecessary elements are specified by XPath.",
                Contact = new OpenApiContact
                {
                    Name = "Source Repository",
                    Url = new Uri("https://github.com/maeda577/RESTfulFeedFilter")
                },
                License = new OpenApiLicense
                {
                    Name = "The MIT License (MIT)",
                    Url = new Uri("https://github.com/maeda577/RESTfulFeedFilter/blob/main/LICENSE")
                }
            });
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(System.IO.Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });

        WebApplication app = builder.Build();
        app.UseHttpLogging();

        // 静的ファイルの提供 https://docs.microsoft.com/ja-jp/aspnet/core/fundamentals/static-files?view=aspnetcore-6.0
        app.UseFileServer();

        // SwaggerのjsonとWebUIを有効化
        app.UseSwagger();
        app.UseSwaggerUI();

        // 属性ルーティングの有効化 https://docs.microsoft.com/ja-jp/aspnet/core/mvc/controllers/routing?view=aspnetcore-6.0#attribute-routing-for-rest-apis
        // APIでは規則ルーティングを使わないらしい
        app.MapControllers();

        app.Run();
    }
}
