using Akka.Actor;
using Akka.Configuration;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using System.Text;

var rabbitHost = Environment.GetEnvironmentVariable("RABBIT_HOST") ?? "localhost";
var rabbitPort = int.Parse(Environment.GetEnvironmentVariable("RABBIT_PORT") ?? "5672");

Console.WriteLine("Connecting to {0}:{1}", rabbitHost, rabbitPort);

var connectionSettings = AmqpConnectionDetails
    .Create(rabbitHost, rabbitPort)
    .WithCredentials(AmqpCredentials.Create("guest", "guest"))
    .WithAutomaticRecoveryEnabled(true)
    .WithNetworkRecoveryInterval(TimeSpan.FromSeconds(1));

var queueName = "myQueue";
//queue declaration
var queueDeclaration = QueueDeclaration
    .Create(queueName)    
    .WithDurable(false)
    .WithAutoDelete(true);

//create sink
var amqpSink = AmqpSink.CreateSimple(
    AmqpSinkSettings
        .Create(connectionSettings)
        .WithRoutingKey(queueName)
        .WithDeclarations(queueDeclaration));

//create source
var amqpSource = AmqpSource.AtMostOnceSource(
    NamedQueueSourceSettings
        .Create(connectionSettings, queueName)
        .WithDeclarations(queueDeclaration), bufferSize: 10);

var config = ConfigurationFactory.ParseString(@"akka.loglevel = INFO");
var configSetup = BootstrapSetup.Create().WithConfig(config);    
var actorSystem = ActorSystem.Create("ActorSystemRabbit", configSetup);
var _mat = actorSystem.Materializer();

//run sink
var input = new[] { "one", "two", "three", "four", "five" };
Source
    .From(input)
    .Select(ByteString.FromString)
    .RunWith(amqpSink, _mat).Wait();

var writeSink = Sink.ForEach<string>(Console.WriteLine);

//run source
await amqpSource
    .Select(m => m.Bytes.ToString(Encoding.UTF8))
    .Take(input.Length)    
    .RunWith(writeSink, _mat);

//Console.WriteLine(string.Join("-", result));
