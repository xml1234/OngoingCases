using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using NetCore_WebApi.Policy;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;

namespace NetCore_WebApi
{
    public class Startup
    {
        private readonly IHostingEnvironment _hostingEnv;

        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            _hostingEnv = env;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            //配置跨域请求
            services.AddCors(options =>
            {
                options.AddPolicy("allow_angularWeb", builder =>
                {
                    //builder.AllowAnyOrigin() //允许任何来源的主机访问
                    builder.WithOrigins("http://localhost:4200")
                        .WithExposedHeaders("X-Pagination")//用于取header
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();//指定处理cookie
                });
            });

            services.AddDbContext<MyContext>(
                options =>
                {
                    options.EnableSensitiveDataLogging();
                    //sql server默认批量操作只支持1000个命令
                    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        opt => opt.MaxBatchSize(2000));
                    //将生命周期改为Transient
                }, ServiceLifetime.Transient);




            //添加策略鉴权模式
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Permission", policy => policy.Requirements.Add(new PolicyRequirement()));
            })
            .AddAuthentication(s =>
            {
                //添加JWT Scheme
                s.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                s.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                s.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            //添加jwt验证：
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,//是否验证失效时间
                    ClockSkew = TimeSpan.FromSeconds(30),

                    ValidateAudience = true,//是否验证Audience
                    //ValidAudience = Const.GetValidudience(),//Audience
                    //这里采用动态验证的方式，在重新登陆时，刷新token，旧token就强制失效了
                    AudienceValidator = (m, n, z) =>
                    {
                        return m != null && m.FirstOrDefault().Equals(Const.ValidAudience);
                    },
                    ValidateIssuer = true,//是否验证Issuer
                    ValidIssuer = Const.Domain,//Issuer，这两项和前面签发jwt的设置一致

                    ValidateIssuerSigningKey = true,//是否验证SecurityKey
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Const.SecurityKey))//拿到SecurityKey
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        //Token expired
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            ;

            //注入授权Handler
            services.AddSingleton<IAuthorizationHandler, PolicyHandler>();



            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSwaggerGen();
            services.ConfigureSwaggerGen(options =>
            {

                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "xxxx接口",
                    Version = "v1",
                    Description = ""
                });
                options.IncludeXmlComments($"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}{_hostingEnv.ApplicationName}.xml");
                //options.IncludeXmlComments(Path.Combine("bin/", "NetCore-WebApi.xml"));
                options.DescribeAllEnumsAsStrings();
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, MyContext myContext)
        {
            myContext.Database.Migrate();
            if (!myContext.Cities.Any())
            {
                myContext.Cities.Add(new Model.City
                {
                    Name = "上海",
                    AreaCode = "1",
                });

                myContext.Cities.Add(new Model.City
                {
                    Name = "北京",
                    AreaCode = "2",
                });
                myContext.Cities.Add(new Model.City
                {
                    Name = "广州",
                    AreaCode = "3",
                });

                myContext.SaveChanges();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseCors("allow_angularWeb");

            app.UseMvc();



            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MsSystem API V1");
            });
        }
    }
}
