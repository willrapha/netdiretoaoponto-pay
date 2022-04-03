using DevFreela.Payments.API.Models;
using DevFreela.Payments.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DevFreela.Payments.API.Consumers
{
    public class ProcessPaymentConsumer : BackgroundService
    {
        private const string QUEUE = "Payments";
        private const string PAYMENT_APPROVED_QUEUE = "PaymentsApproved";
        private readonly IConnection _connection; // conexao
        private readonly IModel _channel; // canal
        private readonly IServiceProvider _serviceProvider; // injeção de dependencia

        public ProcessPaymentConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = factory.CreateConnection(); // cria conexao
            _channel = _connection.CreateModel(); // cria canal

            // cria a fila
            _channel.QueueDeclare(queue: QUEUE,
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            _channel.QueueDeclare(queue: PAYMENT_APPROVED_QUEUE,
                                   durable: false,
                                   exclusive: false,
                                   autoDelete: false,
                                   arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, eventArgs) =>
            {
                var byteArray = eventArgs.Body.ToArray();
                var paymentoInfoJson = Encoding.UTF8.GetString(byteArray);
                var paymentInfo = JsonSerializer.Deserialize<PaymentInfoInputModel>(paymentoInfoJson);

                // processa a mensagem
                ProcessPayment(paymentInfo);

                var paymentApproved = new PaymentApprovedIntegrationEvent(paymentInfo.IdProject);
                var paymentApprovedJson = JsonSerializer.Serialize(paymentApproved);
                var paymentApprovedBytes = Encoding.UTF8.GetBytes(paymentApprovedJson);

                _channel.BasicPublish(exchange: "", // agente que vai rotear a mensagem, "" é o padrão
                                         routingKey: PAYMENT_APPROVED_QUEUE,
                                         basicProperties: null,
                                         body: paymentApprovedBytes);

                // confirma a mensagem recebida
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };

            // autoAck - true qualquer mensagem que recebemos ela seta como ja foi processada
            _channel.BasicConsume(QUEUE, false, consumer);

            return Task.CompletedTask;
        }

        private void ProcessPayment(PaymentInfoInputModel paymentInfo)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var paymentService = scope.ServiceProvider.GetService<IPaymentService>();

                paymentService.ProcessPayment(paymentInfo);
            };
        }
    }
}
