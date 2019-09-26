using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.Unicode;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Starcounter.Nova;
using Starcounter.Nova.Hosting;
using Starcounter.Nova.Binding;
using Starcounter.Nova.Abstractions;

namespace Starcounter.Techempower
{
    public class Startup
    {
        private static readonly IRandom _random = new ThreadSafeRandom();
        private static readonly Task _done = Task.FromResult(0);
        private static readonly JsonSerializer _json = new JsonSerializer();
        private static readonly UTF8Encoding _encoding = new UTF8Encoding(false);

        // Techempower fortune parser does not recognize dash (—) when encoded as &#x2014;.
        // https://github.com/TechEmpower/FrameworkBenchmarks/blob/master/toolset/benchmark/fortune_html_parser.py
        private static readonly TextEncoderSettings _textEncodingSettings = new TextEncoderSettings
        (
            // Basic latin characters.
            UnicodeRanges.BasicLatin,

            // Japanese alphabetical characters.
            UnicodeRanges.Katakana,
            UnicodeRanges.Hiragana,

            // The dash (—) character.
            new UnicodeRange(0x2014, 1)
        );

        private static readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Create(_textEncodingSettings);

        private const int _helloWorldPayloadSize = 27;
        private static readonly byte[] _helloWorldPayload = Encoding.UTF8.GetBytes("Hello, World!");

        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static Task HandleJson(HttpContext httpContext, ITransactor transactor)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.ContentLength = _helloWorldPayloadSize;

            using (var sw = new StreamWriter(httpContext.Response.Body, _encoding, bufferSize: _helloWorldPayloadSize))
            {
                _json.Serialize(sw, new { message = "Hello, World!" });
            }

            return _done;
        }

        static private int GetRandomWantedId()
        {
            return _random.Next(1, World.Count + 1);
        }

        static private IWorld GetRandomWorld(IDatabaseContext db, int wantedId)
        {
            return db.Sql<IWorld>("SELECT w FROM Starcounter.Techempower.World w WHERE w.Id = ?", wantedId).First();
        }

        public Task HandleDb(HttpContext httpContext, ITransactor transactor)
        {
            string result = null;
            int wantedId = GetRandomWantedId();

            transactor.Transact(db =>
            {
                IWorld w = GetRandomWorld(db, wantedId);

                if (w != null)
                    result = JsonConvert.SerializeObject(w, _jsonSerializerSettings);
            });

            if (result == null)
            {
                result = "410 Gone\r\n";
                httpContext.Response.StatusCode = StatusCodes.Status410Gone;
                httpContext.Response.ContentType = "text/plain";
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                httpContext.Response.ContentType = "application/json";
            }

            httpContext.Response.ContentLength = result.Length;
            return httpContext.Response.WriteAsync(result);
        }

        public int GetQueriesValue(HttpContext httpContext)
        {
            int queries;
            StringValues values;

            if (!httpContext.Request.Query.TryGetValue("queries", out values))
                return 1;

            if (!int.TryParse(values[0], out queries))
                return 1;

            if (queries < 1)
                queries = 1;

            if (queries > 500)
                queries = 500;

            return queries;
        }

        public async Task HandleQueries(HttpContext httpContext, ITransactor transactor)
        {
            string result = null;
            int queries = GetQueriesValue(httpContext);
            IWorld[] results = new IWorld[queries];
            int[] wantedIds = new int[queries];

            for (int i = 0; i < queries; i++)
            {
                wantedIds[i] = GetRandomWantedId();
            }

            await transactor.TransactAsync(db =>
            {
                for (int i = 0; i < queries; i++)
                {
                    results[i] = new WorldOrm(GetRandomWorld(db, wantedIds[i]));
                }
            });

            result = JsonConvert.SerializeObject(results, _jsonSerializerSettings);

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.ContentLength = result.Length;

            await httpContext.Response.WriteAsync(result);
        }

        public static async Task RenderFortunesHtml(IEnumerable<IFortune> model, HttpContext httpContext, HtmlEncoder htmlEncoder)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "text/html; charset=UTF-8";

            await httpContext.Response.WriteAsync("<!DOCTYPE html><html><head><title>Fortunes</title></head><body><table><tr><th>id</th><th>message</th></tr>");

            foreach (IFortune item in model)
            {
                await httpContext.Response.WriteAsync($"<tr><td>{htmlEncoder.Encode(item.Id.ToString())}</td><td>{htmlEncoder.Encode(item.Message)}</td></tr>");
            }

            await httpContext.Response.WriteAsync("</table></body></html>");
        }

        public async Task HandleFortunes(HttpContext httpContext, ITransactor transactor)
        {
            List<IFortune> fortunes = null;

            await transactor.TransactAsync(db =>
            {
                fortunes = new List<IFortune>();

                foreach (Fortune f in db.Sql<Fortune>("SELECT f FROM Starcounter.Techempower.Fortune f"))
                {
                    fortunes.Add(new FortuneOrm(f));
                }

                Debug.Assert(fortunes.Count == Fortune.Count);
                fortunes.Add(new FortuneOrm() { Message = "Additional fortune added at request time." });
            });

            fortunes.Sort();

            await RenderFortunesHtml(fortunes, httpContext, _htmlEncoder);
        }

        public async Task HandleUpdates(HttpContext httpContext, ITransactor transactor)
        {
            string result = null;
            int queries = GetQueriesValue(httpContext);
            TransactOptions opts = new TransactOptions(OnConflict);
            IWorld[] results = new IWorld[queries];
            int[] wantedIds = new int[queries];

            for (int i = 0; i < queries; i++)
            {
                wantedIds[i] = GetRandomWantedId();
            }

            await transactor.TransactAsync(db =>
            {
                for (int i = 0; i < queries; i++)
                {
                    IWorld w = GetRandomWorld(db, wantedIds[i]);
                    w.RandomNumber = _random.Next(1, World.Count + 1);
                    results[i] = new WorldOrm(w);
                }
            }, opts);

            result = JsonConvert.SerializeObject(results, _jsonSerializerSettings);

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.ContentLength = result.Length;

            await httpContext.Response.WriteAsync(result);
        }

        private static bool OnConflict(object _)
        {
            // Repeat transaction until it succeeds.
            return true;
        }

        public Task HandlePlaintext(HttpContext httpContext, ITransactor transactor)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "text/plain";
            httpContext.Response.Headers["Content-Length"] = "13";

            return httpContext.Response.Body.WriteAsync(_helloWorldPayload, 0, _helloWorldPayload.Length);
        }

        /// <summary>
        /// This function is needed to implement `update` action verification by Techempower.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task HandleSelectWorlds(HttpContext httpContext, ITransactor transactor)
        {
            string json = null;
            Dictionary<int, int> data = null;

            await transactor.TransactAsync(db =>
            {
                data = db.Sql<World>("SELECT w FROM Starcounter.Techempower.World w").ToDictionary(k => k.Id, v => v.RandomNumber);
            });

            json = JsonConvert.SerializeObject(data, _jsonSerializerSettings);
            await httpContext.Response.WriteAsync(json);
        }

        public void Configure(IApplicationBuilder app, IServiceProvider services)
        {
            Dictionary<string, Func<HttpContext, ITransactor, Task>> _routes = new Dictionary<string, Func<HttpContext, ITransactor, Task>>();

            _routes["/json"] = HandleJson;
            _routes["/db"] = HandleDb;
            _routes["/queries"] = HandleQueries;
            _routes["/fortunes"] = HandleFortunes;
            _routes["/updates"] = HandleUpdates;
            _routes["/plaintext"] = HandlePlaintext;
            _routes["/select/worlds"] = HandleSelectWorlds;

            ITransactor transactor = services.GetRequiredService<ITransactor>();
            World.Populate(transactor);
            Fortune.Populate(transactor);

            app.Run(httpContext =>
            {
                Func<HttpContext, ITransactor, Task> action;

                if (_routes.TryGetValue(httpContext.Request.Path.Value, out action))
                {
                    return action.Invoke(httpContext, transactor);
                }

                string response = "404 Not Found";
                httpContext.Response.StatusCode = 404;
                httpContext.Response.ContentLength = response.Length;
                httpContext.Response.ContentType = "text/plain";
                return httpContext.Response.WriteAsync(response);
            });
        }

        public static void Main()
        {
            string listenAddress = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

            if (string.IsNullOrWhiteSpace(listenAddress))
            {
                listenAddress = "http://*:8080";
            }

            string databaseName = $"Techempower{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            string connectionString = $@"Database=./.database/{databaseName};OpenMode=CreateIfNotExists;StartMode=StartIfNotRunning;StopMode=IfWeStarted";

            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    // options.UseHttps("testCert.pfx", "testPassword");
                })
                .UseUrls(listenAddress)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .ConfigureLogging((logging) =>
                {
                    logging.AddFilter(nameof(Microsoft), LogLevel.Error);
                    logging.AddConsole();
                })
                .ConfigureServices((services) =>
                {
                    services.AddStarcounter(connectionString)
                    .Configure<TypeBindingOptions>(x =>
                    {
                        x.Types = new Type[] { typeof(Fortune), typeof(World) };
                    });
                })
                .Build();

            Console.WriteLine($"Starcounter.Techempower is running and listening {listenAddress} address.");

            host.Run();
        }
    }
}