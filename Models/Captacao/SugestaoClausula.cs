using TaesaCore.Models;

namespace APIGestor.Models.Captacao
{
    public class SugestaoClausula : BaseEntity
    {
        public int ClausulaId { get; set; }
        public Clausula Clausula { get; set; }
        public int FornecedorId { get; set; }
        public Fornecedores.Fornecedor Fornecedor { get; set; }
        public string Conteudo { get; set; }
    }
}