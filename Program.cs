using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;
using System.IO;
// using System.IO.MemoryMappedFiles;
using System.Reactive.Linq;
using System.Threading;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Collections;
using System.Xml.Linq;
using System.Net.Sockets;
// using CsvHelper;
// using NetMQ.Sockets;
// using NetMQ;
// using System.Threading;
// using StackExchange.Redis;
// using Npgsql;
using System.Runtime.InteropServices;
using Amqp;
using RabbitMQ.Client;
using MySql.Data.MySqlClient;
using Nest;
using ImageProcessorCore;
// using System.Drawing;
using GraphQL;
using GraphQL.Http;
using GraphQL.Types;

namespace ConsoleApplication
{
    class Something
    {
        private int _currentSpeed;
        public int CurrentSpeed
        {
            get
            {
                return _currentSpeed;
            }

            set
            {
                if (value < 0) return;
                if (value > 120) return;

                _currentSpeed = value;
            }
        }
    }

    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Occupation { get; set; }
        public int Age { get; set; }
    }

    public class FullName
    {
        public string First { get; set; }
        public string Last { get; set; }
    }

    public static class ExtensionMethods
    {
        public static int PlusFive(this int input)
        {
            return input + 5;
        }
    }

    public class CustomEventArgs : EventArgs
    {
        private string message;
        public CustomEventArgs(string s)
        {
            message = s;
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }
    }

    public class Publisher
    {
        public event EventHandler<CustomEventArgs> RaiseCustomEvent;

        protected virtual void OnRaiseCustomEvent(CustomEventArgs e)
        {
            EventHandler<CustomEventArgs> handler = RaiseCustomEvent;
            if (handler != null)
            {
                e.Message += String.Format(" at {0}", DateTime.Now.ToString());

                handler(this, e);
            }
        }

        public void DoSomething()
        {
            OnRaiseCustomEvent(new CustomEventArgs("Did something"));
        }
    }

    class Subscriber
    {
        private string id;
        public Subscriber(string ID, Publisher pub)
        {
            id = ID;
            // Subscribe to the event using C# 2.0 syntax
            pub.RaiseCustomEvent += HandleCustomEvent;
        }

        // Define what actions to take when the event is raised.
        void HandleCustomEvent(object sender, CustomEventArgs e)
        {
            Console.WriteLine(id + " received this message: {0}", e.Message);
        }
    }

    public class Program
    {
        enum Days { Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday };

        public static List<Person> GenerateListOfPeople()
        {
            Console.WriteLine($"--Friday--: {Days.Friday}");
            var people = new List<Person>();

            people.Add(new Person { FirstName = "Eric", LastName = "Fleming", Occupation = "Dev", Age = 24 });
            people.Add(new Person { FirstName = "Steve", LastName = "Smith", Occupation = "Manager", Age = 40 });
            people.Add(new Person { FirstName = "Brendan", LastName = "Enrick", Occupation = "Dev", Age = 30 });
            people.Add(new Person { FirstName = "Jane", LastName = "Doe", Occupation = "Dev", Age = 35 });
            people.Add(new Person { FirstName = "Samantha", LastName = "Jones", Occupation = "Dev", Age = 24 });

            return people;
        }

        static async Task Run()
        {
            string address = "amqp://guest:guest@localhost:5672";

            Connection connection = await Connection.Factory.CreateAsync(new Address(address));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "test-sender", "q1");

            Message message1 = new Message("Hello AMQP!");
            await sender.SendAsync(message1);

            ReceiverLink receiver = new ReceiverLink(session, "test-receiver", "q1");
            Message message2 = await receiver.ReceiveAsync();
            Console.WriteLine(message2.GetBody<string>());
            receiver.Accept(message2);

            await sender.CloseAsync();
            await receiver.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();
        }

        static void MySqlTest()
        {
            var connectionString = "server=localhost;userid=root;pwd=root;port=3306;database=test;sslmode=none;";
            var mConn = new MySqlConnection(connectionString);

            try
            {
                Console.WriteLine("Connecting to MySQL...");
                mConn.Open();
                // Perform database operations
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            mConn.Close();
            Console.WriteLine("Done.");
        }
        static void RabbitMQRun()
        {
            var factory = new RabbitMQ.Client.ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                    routingKey: "hello",
                                    basicProperties: null,
                                    body: body);
                Console.WriteLine(" [x] Sent {0}", message);
            }
        }

        // Import the libc and define the method corresponding to the native function.
        [DllImport("libSystem.dylib")]
        private static extern int getpid();

        private class Tweet
        {
            public int Id;
            public string User;
            public DateTime PostDate;
            public string Message;
        }

        static void eSearch()
        {
            // Docs: https://github.com/elastic/elasticsearch-net
            var node = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(node);
            var client = new ElasticClient(settings);

            var tweet = new Tweet
            {
                Id = 1,
                User = "kimchy",
                PostDate = new DateTime(2009, 11, 15),
                Message = "Trying out NEST, so far so good?"
            };

            var response = client.Index(tweet, idx => idx.Index("mytweetindex"));
            // var response = client.IndexAsync(tweet, idx => idx.Index("mytweetindex"));

            var cg = client.Get<Tweet>(1, idx => idx.Index("mytweetindex")); // returns an IGetResponse mapped 1-to-1 with the Elasticsearch JSON response
            var tw = cg.Source; // the original document

            var rp = client.Search<Tweet>(s => s
                .From(0)
                .Size(10)
                .Query(q =>
                        q.Term(t => t.User, "kimchy")
                        || q.Match(mq => mq.Field(f => f.User).Query("nest"))
                    )
                );
        }

        // CoreCompact -> System.Drawing
        // static void DrawingTest() {
        //     const int size = 150;
        //     const int quality = 75;

        //     using (var image = new Bitmap(System.Drawing.Image.FromFile("piyush.jpg")))
        //     {
        //         int width, height;
        //         if (image.Width > image.Height)
        //         {
        //             width = size;
        //             height = Convert.ToInt32(image.Height * size / (double)image.Width);
        //         }
        //         else
        //         {
        //             width = Convert.ToInt32(image.Width * size / (double)image.Height);
        //             height = size;
        //         }
        //         var resized = new Bitmap(width, height);
        //         Console.WriteLine(resized);
        //         Console.WriteLine(resized.Size.ToJson());
        //         using (var graphics = Graphics.FromImage(resized))
        //         {
        //             graphics.CompositingQuality = CompositingQuality.HighSpeed;
        //             graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //             graphics.CompositingMode = CompositingMode.SourceCopy;
        //             graphics.DrawImage(image, 0, 0, width, height);
        //             Console.WriteLine(graphics);
        //             using (var output = File.Open("drawing.jpg", FileMode.Create))
        //             {
        //                 var qualityParamId = Encoder.Quality;
        //                 var encoderParameters = new EncoderParameters(1);
        //                 encoderParameters.Param[0] = new EncoderParameter(qualityParamId, quality);
        //                 var codec = ImageCodecInfo.GetImageDecoders()
        //                     .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        //                 resized.Save(output, codec, encoderParameters);
        //             }
        //         }
        //     }
        // }

        public static void Main(string[] args)
        {

            // RabbitMQRun(); // brew install rabbitmq && rabbitmq-server
            // MySqlTest();

            // Invoke the function and get the process ID.
            int pid = getpid();
            Console.WriteLine(pid);

            // Run().Wait(); // AMQP # brew install activemq && activemq start

            Console.WriteLine("Hello World!");
            string name = "Piyush Chauhan";
            int a = 23;
            Console.WriteLine(a.ToString(), name);

            List<string> names = new List<string>() { "Piyush", "Kartikey", "Mithun" };
            names.Sort();
            Console.WriteLine(names);
            int found = names.BinarySearch("Kartikey");
            Console.WriteLine(found);

            long i = 0;
            while (i < 1000)
            {
                found = names.BinarySearch("Piyush");
                i++;
            }

            Console.WriteLine(found);
            var s = new Something();
            Console.WriteLine(s.CurrentSpeed);
            s.CurrentSpeed += 1;
            Console.WriteLine(s.CurrentSpeed);
            s.CurrentSpeed += 5;
            Console.WriteLine(s.CurrentSpeed);
            s.CurrentSpeed += 1000;
            Console.WriteLine(s.CurrentSpeed);

            int numberToGuess = new Random().Next(1, 101);
            Console.WriteLine(numberToGuess);

            var people = GenerateListOfPeople();
            var peopleOverTheAgeOf30 = people.Where(x => x.Age > 30);
            IEnumerable<Person> afterTwo = people.Skip(2);
            IEnumerable<Person> takeTwo = people.Take(2);
            IEnumerable<string> allFirstNames = people.Select(x => x.FirstName);

            IEnumerable<FullName> allFullNames = people.Select(x => new FullName { First = x.FirstName, Last = x.LastName });

            foreach (var fullName in allFullNames)
            {
                Console.WriteLine($"{fullName.Last}, {fullName.First}");
            }

            people.Sort((p1, p2) => p1.Age.CompareTo(p2.Age));

            foreach (var person in people)
            {
                Console.Write($"|| {person.LastName}, {person.FirstName} :{person.Age} ");
            }
            Console.Write("||");

            var b = new BigInteger(9879879878);
            var c = BigInteger.Pow(b, 230);
            Console.WriteLine(c);

            Func<int, int> addOne = x => x + 1;
            Console.WriteLine(addOne(4));

            var currentTime = DateTime.Now;
            var today = DateTime.Today;
            Console.WriteLine(today.AddDays(2));
            Console.WriteLine(currentTime.AddHours(5));

            // Task<int> urlLength = AccessTheWebAsync();
            // Console.WriteLine($"urlLength: {urlLength.Result}");

            foreach (int number in SomeNumbers())
            {
                Console.Write(number.ToString() + " ");
            }
            Console.WriteLine();

            foreach (int number in EvenSequence(5, 18))
            {
                Console.Write(number.ToString() + " ");
            }
            Console.WriteLine();

            Console.WriteLine($"fact: {factorial(10)}");

            var im = ImmutableArray.Create<int>(1, 2, 3);
            var rs = im.Add(4);
            // original not modified
            foreach (var m in im)
            {
                Console.Write($"{m},");
            }
            Console.WriteLine();

            // The new list from original
            foreach (var m in rs)
            {
                Console.Write($"{m},");
            }
            Console.WriteLine();

            Func<int, Func<int, int>> curriedAdd = x => y => x + y;
            var retAdd = curriedAdd(2);
            Console.WriteLine($"curry addition: {retAdd(5)}");

            //Map Functions.
            int[] myList = { 10, 20, 30, 40, 60 };
            var myDblList = myList.Select(item => item * 2);
            var myDblList1 = from item in myList select item * 2;
            Console.WriteLine("Using Lambda Expressions");
            foreach (int item in myDblList)
            {
                Console.WriteLine(item.ToString());
            }
            Console.WriteLine("Using LINQ");
            foreach (int item in myDblList1)
            {
                Console.WriteLine(item.ToString());
            }

            var l = new KeyValuePair<string, KeyValuePair<int, int>>(
                "Piyush",
                new KeyValuePair<int, int>(2, 3)
            );

            string mult = @"
My name is Piyush.
Currently, studying at USYD.
Major: Data Analytics
Master in Information Technology and
Master in Information Technology Management
            ";

            var re = mult.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine(re[0]);

            int[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int[] percentages = { 30, 60, 10 };
            var distributions = new List<IEnumerable<int>>();
            var numbersOfItems = percentages.Select(n => (int)Math.Floor((double)values.Length * n / 100)).ToArray();
            Console.WriteLine($"values Average is : {values.Average()}");
            foreach (var dist in distributions)
            {
                Console.Write($"dist: {dist}");
            }
            Console.WriteLine();

            Action<Person> print = person => Console.WriteLine("Name: {0}, Age: {1}", person.FirstName + " " + person.LastName, person.Age);
            people
                .FindAll(person => person.Age >= 35)
                .ForEach(print);

            Console.Write(typeof(Person));

            var ts = new Tuple<string, int, double, byte>("Piyush", 2000, 3.2, 3);
            var tp = new Tuple<string, Tuple<int, int>>(
                "Piyush",
                new Tuple<int, int>(2, 3)
            );
            var tc = Tuple.Create("Kartikey", 48894, 4.5, 12);
            Console.WriteLine(tp.Item2.Item1.GetType());
            Console.WriteLine(tc.Item3);

            var retValues = new List<int>();
            var pRet = Parallel.ForEach(values, (int value) =>
            {
                retValues.Add(value * 2);
            });

            // if(pRet.IsCompleted) {
            retValues.ForEach(v => Console.Write($"{v}, "));
            // }
            Console.WriteLine();

            values.ToList().ForEach(v => Console.Write($"{v}:"));
            Console.WriteLine();

            var a1 = new IPAddress(new byte[] { 101, 102, 103, 104 });
            var a2 = IPAddress.Parse("101.102.103.104");
            Console.WriteLine(a1.Equals(a2));
            Console.WriteLine(a1.AddressFamily);

            var a3 = IPAddress.Parse("[3EA0:FFFF:198A:E4A3:4FF2:54fA:41BC:8D31]");
            Console.WriteLine(a3.AddressFamily);

            var p = Process.Start("vim");
            p.Kill();

            var fi = new FileInfo("FileInfo.txt");
            Console.WriteLine(fi.Exists);
            using (var w = fi.CreateText())
                w.Write("Some Text");

            Console.WriteLine(fi.Exists);
            fi.Refresh();
            Console.WriteLine(fi.Exists);

            Console.WriteLine(fi.Name);
            Console.WriteLine(fi.FullName);
            Console.WriteLine(fi.DirectoryName);
            Console.WriteLine(fi.Directory.Name);
            Console.WriteLine(fi.Extension);
            Console.WriteLine(fi.Length);
            Console.WriteLine(fi.Attributes);
            Console.WriteLine(fi.CreationTime);

            var topSide = BorderSide.Top;
            var isTop = (topSide == BorderSide.Top);

            var r = Enumerable.Range(4, 10);
            r.ToList().ForEach(m => Console.Write($"{m}, "));
            Console.WriteLine();

            var st = new Stack<int>();
            st.Push(23);
            st.Push(55);
            Console.WriteLine(st.First());
            Console.WriteLine(st.Average());

            Del handler = DelegateMethod;
            handler("Hello from the delegate");

            handler = delegate (string nm) { Console.WriteLine("Notification received for {0}", nm); };
            handler("Piyush");

            var pub = new Publisher();
            var sub1 = new Subscriber("sub1", pub);
            var sub2 = new Subscriber("sub2", pub);

            pub.DoSomething();

            // using (var server = new ResponseSocket())
            // {
            //     server.Bind("tcp://*:5555");

            //     while (true)
            //     {
            //         var message = server.ReceiveFrameString();

            //         Console.WriteLine("Received {0}", message);

            //         // processing the request
            //         Thread.Sleep(100);

            //         Console.WriteLine("Sending World");
            //         server.SendFrame("World");
            //     }
            // }

            // StartBackgroundWork();

            // RunMongo();

            // new FileStream("test.txt", FileMode.Create);

            // File Processing commands
            // if (File.Exists("test.txt"))
            // {
            //     using (var sr = new StreamReader(File.OpenRead("test.txt")))
            //     {
            //         string newContent = sr.ReadLine();
            //         while (!sr.EndOfStream)
            //         {
            //             Console.WriteLine(newContent);
            //             newContent = sr.ReadLine();
            //         }
            //         Console.WriteLine(newContent);
            //     }

            //     using (var at = File.AppendText("test.txt"))
            //     {
            //         at.Write($"{at.NewLine}Hello World");
            //     }
            // }

            var sb = new StringBuilder("Piyush Chauhan");
            sb.Append(" | Age: 26");
            Console.WriteLine(sb);

            // Console.ReadKey();
            Foo(x: 1, y: 2);
            Bar(d: 3);

            float? quality = null;
            Console.WriteLine($"quality: {quality} is null");

            object[] moreTypes = { "string", 123, true };
            Console.WriteLine($"{moreTypes[0]}, {moreTypes[1]}, {moreTypes[2]}");

            string[] names2 = { "Tom", "Dick", "Harry", "Mary", "Jay" };
            var query = from n in names2
                        where n.EndsWith("y")
                        select n;
            printNames2(query);

            var query2 = from n in names2
                         where n.Length > 3
                         let u = n.ToUpper()
                         where u.EndsWith("Y")
                         select u;
            printNames2(query2);

            var names10 = new string[] { "Harry", "Mary", "Jay" };
            var names11 = new string[] { "Mary", "Jay" };

            foreach (var n in names10.Except(names11))
            {
                Console.Write($"{n} ");
            }

            var person1 = new Person();
            person1.FirstName = "Piyush";
            person1.LastName = "Chauhan";
            person1.Age = 25;
            person1.Occupation = "Software Engineer";

            string output = JsonConvert.SerializeObject(person1);
            Console.WriteLine(output);

            // var csv = new CsvReader(File.OpenText("persons.csv"));
            // csv.Configuration.HasHeaderRecord = true;
            // var people1 = csv.GetRecords<Person>();
            // Console.WriteLine(people1.ToJson());

            // Console.WriteLine(person1.ToJson()); // JSON Serializer by MongoDB

            // InsertParsons();

            FindParsons();

            var bits = new BitArray(8);
            bits.SetAll(true);
            bits.Set(1, false);
            bits[5] = false;
            bits[7] = false;
            Console.Write("Initialized: ");
            DisplayBits(bits);
            Console.WriteLine();

            var xmlCategories = XElement.Load("Categories.xml");
            var expr = from xc in xmlCategories.Elements("Category")
                       select new
                       {
                           Name = xc.Attribute("Name").Value,
                           Description = xc.Attribute("Description").Value
                       };

            // foreach (var xc in xmlCategories.Elements("Category")) {
            //     var xcName = xc.Attribute("Name").Value;
            //     var xcDesc = xc.Attribute("Description").Value;
            //     Console.WriteLine($"Name: {xcName}, Description: {xcDesc}");
            // }

            Console.WriteLine(expr.ToJson());

            var listener = new TcpListener(IPAddress.Loopback, 5555);
            // listener.Start(); // Not joined with main thread, have to join it
            // Console.WriteLine("Listening on Port 5555");

            // using (var conn = new NpgsqlConnection("Host=localhost;Username=piyushchauhan;Password=;Database=piyushchauhan")) {
            //     conn.Open();

            //     conn.Close();
            // }
            OpenImage();

            // DrawingTest();

            GraphQLTest();
        }

        static async void GraphQLTest()
        {
            Console.WriteLine("Hello GraphQL");

            var schema = new Schema { Query = new StarWarsQuery() };

            var result = await new DocumentExecuter().ExecuteAsync( _ =>
            {
                _.Schema = schema;
                _.Query = @"
                    query {
                        hero {
                            id
                            name
                        }
                    }
                ";
            }).ConfigureAwait(false);

            var json = new DocumentWriter(indent: true).Write(result);

            Console.WriteLine(json);
        }

        class StarWarsQuery : ObjectGraphType
        {
            public StarWarsQuery()
            {
                Field<DroidType>(
                    "hero",
                    resolve: context => new Droid { Id = "1", Name = "R2 - D2" }
                );
            }
        }

        class Droid
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        class DroidType : ObjectGraphType<Droid>
        {
            public DroidType()
            {
                Field(x => x.Id).Description("The Id of the Droid.");
                Field(x => x.Name, nullable: true).Description("The name of the Droid.");
            }
        }

        static void OpenImage()
        {
            var f = File.OpenRead("piyush.jpg");
            var output = File.OpenWrite("grayscale.jpg");
            var image = new ImageProcessorCore.Image(f);
            image
                .Resize(image.Width / 2, image.Height / 2)
                .Grayscale()
                .Save(output);
            Console.WriteLine(output.Name);
        }

        public static void DisplayBits(BitArray bits)
        {
            foreach (bool bit in bits)
            {
                Console.Write(bit ? 1 : 0);
            }
        }

        public static void FindParsons()
        {
            try
            {
                var client = new MongoClient();
                var database = client.GetDatabase("foo");

                var collection = database.GetCollection<Parson>("parsons");

                var doc = collection.Find(p => p.Name == "Piyush");
                Console.WriteLine(doc.ToString());
            }
            catch (MongoClientException e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Insert a person into mongodb database 'foo' with collection 'parsons'
        /// </summary>
        /// <returns>void</returns>
        public static void InsertParsons()
        {
            try
            {
                var client = new MongoClient();
                var database = client.GetDatabase("foo");

                var collection = database.GetCollection<BsonDocument>("parsons");

                var parson = new Parson();
                parson.Name = "Piyush";
                parson.Age = 26;
                parson.Pets = new List<Pet>();

                var pet = new Pet();
                pet.Name = "Follow Dat";

                var document = parson.ToBsonDocument();

                collection.InsertOne(document);
            }
            catch (MongoClientException e)
            {
                Console.WriteLine(e);
            }
        }

        class Parson
        {
            public string Name { get; set; }

            public int Age { get; set; }

            public IEnumerable<Pet> Pets { get; set; }

            public int[] FavoriteNumbers { get; set; }

            public HashSet<string> FavoriteNames { get; set; }

            public DateTime CreatedAtUtc { get; set; }

            public int PermissionFlags { get; set; }
        }

        class Pet
        {
            public string Name { get; set; }
        }

        public static void RunMongo()
        {
            try
            {
                var client = new MongoClient();
                var database = client.GetDatabase("foo");
                var collection = database.GetCollection<BsonDocument>("bar");

                var document = new BsonDocument
                {
                    { "name", "MongoDB" },
                    { "type", "Database" },
                    { "count", 1 },
                    { "info", new BsonDocument
                        {
                            { "x", 203 },
                            { "y", 102 }
                        }}
                };

                collection.InsertOne(document);
            }
            catch (MongoClientException e)
            {
                Console.WriteLine(e);
            }
        }

        private static void printNames2(IEnumerable<string> query)
        {
            foreach (var qy in query)
            {
                Console.Write($"{qy} ");
            }
            Console.WriteLine();
        }

        private static void Foo(int x, int y)
        {
            Console.WriteLine($"Named arguments: {x}, {y}");
        }

        private static void Bar(int a = 0, int b = 0, int c = 0, int d = 0)
        {
            Console.WriteLine($"Default Named Arguments: {a}, {b}, {c}, {d}");
        }

        public static void StartBackgroundWork()
        {
            Console.WriteLine("Shows use of Start to start on a background thread:");
            var o = Observable.Start(() =>
            {
                //This starts on a background thread.
                Console.WriteLine("From background thread. Does not block main thread.");
                Console.WriteLine("Calculating...");
                Thread.Sleep(3000);
                Console.WriteLine("Background work completed.");
            }).Finally(() => Console.WriteLine("Main thread completed."));
            Console.WriteLine("\r\n\t In Main Thread...\r\n");
            o.Wait();   // Wait for completion of background operation.
        }

        public delegate void Del(string message);

        public static void DelegateMethod(string message)
        {
            Console.WriteLine(message);
        }

        public enum BorderSide { Left, Right, Top, Bottom }

        static async Task<int> AccessTheWebAsync()
        {
            HttpClient client = new HttpClient();

            Task<string> getStringTask = client.GetStringAsync("https://www.otherlevels.com");
            Task<string> microsoftTask = client.GetStringAsync("http://msdn.microsoft.com");

            for (int i = 0; i < 100; i++)
            {
                Console.Write($"i: {i}, ");
            }
            Console.WriteLine();

            string urlContents = await getStringTask;
            string mLen = await microsoftTask;
            Console.WriteLine($"mLen: {mLen.Length}");

            return urlContents.Length;
        }

        public static IEnumerable<int> SomeNumbers()
        {
            yield return 3;
            yield return 5;
            yield return 8;
        }

        public static IEnumerable<int> EvenSequence(int firstNumber, int lastNumber)
        {
            for (int number = firstNumber; number <= lastNumber; number++)
            {
                if (number % 2 == 0)
                {
                    yield return number;
                }
            }
        }

        /**
         * Tail Call Optimized factorial function
         * @returns uint
         * Follows accumulator pattern
         */
        private static uint _fact(uint n, uint acc)
        {
            if (n == 0)
            {
                return acc;
            }
            return _fact(n - 1, acc * n);
        }

        public static uint factorial(uint n)
        {
            return _fact(n, 1);
        }
    }

    public class ExtensionMethodsPlusFiveShould
    {
        [Fact]
        public void ReturnFiveMoreThanInput()
        {
            //Arrange
            int input = 10;
            int expectedResult = 15;

            //Act
            int actualResult = input.PlusFive();

            //Assert
            Assert.Equal(expectedResult, actualResult);
        }
    }

    public abstract class Shape
    {
        public abstract int Perimeter();
    }

    public interface ICountry
    {
        int GetBills();
        int FirstName { get; }
    }

    public class Rectangle : Shape
    {
        public int Height { get; set; }
        public int Width { get; set; }

        public override int Perimeter()
        {
            return (Height + Width) / 2;
        }
    }

    public class Triangle : Shape
    {
        public int Side1 { get; set; }
        public int Side2 { get; set; }
        public int Side3 { get; set; }

        public override int Perimeter()
        {
            return Side1 + Side2 + Side3;
        }

        public override string ToString()
        {
            return $"{Side1}, {Side2}, {Side3}";
        }
    }
}
