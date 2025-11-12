using ZavaPoker.WebApi.Models;

namespace ZavaPoker.WebApi.Services
{
    public class AdoService
    {
        public Task<AdoWorkItem> GetWorkItemDetails(string id)
        {
            Console.WriteLine($"Simulando fetch da API do ADO para o item: {id}");

            var mockItem = new AdoWorkItem(id, "Product Backlog Item", "Como utilizador, quero poder votar num item para que a equipa saiba o esforço", $"https://dev.azure.com/seu-org/seu-projeto/_workitems/edit/{id}", "Esta é a descrição do PBI <strong>12345</strong>. Devemos detalhar os critérios de aceitação aqui.");
            
            return Task.FromResult(mockItem);
        }
    }
}