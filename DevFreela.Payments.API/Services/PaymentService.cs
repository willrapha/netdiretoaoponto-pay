using DevFreela.Payments.API.Models;
using System.Threading.Tasks;

namespace DevFreela.Payments.API.Services
{
    public class PaymentService : IPaymentService
    {
        public async Task<bool> ProcessPayment(PaymentInfoInputModel paymentInfoInputModel)
        {
            return true;
        }
    }
}
