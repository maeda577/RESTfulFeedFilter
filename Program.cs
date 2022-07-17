using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Rff;

internal class Program
{
    private static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // 標準の出力を切ってXMLに限定する https://docs.microsoft.com/ja-jp/aspnet/core/web-api/advanced/formatting?view=aspnetcore-6.0
        builder.Services.AddControllers(options =>
        {
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
            options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();
        }).AddXmlSerializerFormatters();

        // Swaggerの設定 https://docs.microsoft.com/ja-jp/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-6.0&tabs=visual-studio
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "RESTful Feed Filter",
                Description = "Filtering API for rss/atom feeds",
                Contact = new OpenApiContact
                {
                    Name = "Source Repository",
                    Url = new Uri("https://github.com/maeda577/RESTfulFeedFilter")
                },
                License = new OpenApiLicense
                {
                    Name = "The Unlicense",
                    Url = new Uri("https://github.com/maeda577/RESTfulFeedFilter/blob/main/LICENSE")
                }
            });
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(System.IO.Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });

        WebApplication app = builder.Build();

        // SwaggerのjsonとWebUIを有効化
        app.UseSwagger();
        app.UseSwaggerUI();

        // 属性ルーティングの有効化 https://docs.microsoft.com/ja-jp/aspnet/core/mvc/controllers/routing?view=aspnetcore-6.0#attribute-routing-for-rest-apis
        // APIでは規則ルーティングを使わないらしい
        app.MapControllers();

        app.Run();
    }
}
