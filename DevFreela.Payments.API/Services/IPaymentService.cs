using DevFreela.Payments.API.Models;
using System.Threading.Tasks;

namespace DevFreela.Payments.API.Services
{
    public interface IPaymentService
    {
        Task<bool> ProcessPayment(PaymentInfoInputModel paymentInfoInputModel);
    }
}
