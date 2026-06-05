using Microsoft.AspNetCore.Identity;

namespace TodoList.Api.Data.Entities;

///
/// <summary>
/// Objetivo: Representar um usuário da aplicação como ele é PERSISTIDO pelo ASP.NET Core Identity —
/// a entidade mapeada para a tabela "AspNetUsers", base do login exigido pelo docs/IDEA.md.
///
/// Descrição:
/// 1. Herda de <see cref="IdentityUser{TKey}"/> com chave <see cref="Guid"/>, reaproveitando os campos que o Identity já fornece
/// (UserName, NormalizedUserName, Email, PasswordHash, etc.) e o hashing de senha embutido.
/// 2. Não acrescenta campos próprios nesta etapa: o projeto autentica por nome de usuário e não precisa de dados de perfil adicionais.
/// </summary>
///
/// <remarks>
/// Restrições:
/// - A chave é <see cref="Guid"/> (e não a <c>string</c> padrão do Identity) DE PROPÓSITO: assim ela casa com as colunas
/// <c>Guid?</c> já existentes em <see cref="TaskItem.ResponsibleUserId"/>/<see cref="TaskItem.CreatedByUserId"/>, evitando reconciliação de tipo.
/// - A senha NUNCA é guardada em texto puro: o Identity grava apenas o <c>PasswordHash</c> (ver docs/IDEA.md, pergunta 7).
/// </remarks>
///
public sealed class AppUser : IdentityUser<Guid>
{
}
