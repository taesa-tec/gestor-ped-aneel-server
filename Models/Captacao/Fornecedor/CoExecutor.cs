using TaesaCore.Models;

namespace APIGestor.Models.Captacao.Fornecedor
{
    public class CoExecutor : BaseEntity
    {
        public int FornecedorId { get; set; }
        public Fornecedor Fornecedor { get; set; }
        public string CNPJ { get; set; }
        public string UF { get; set; }
        public string RazaoSocial { get; set; }
        public Contrato Contrato { get; set; }
    }
}