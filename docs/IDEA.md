Esse arquivo lista os requisitos estabelecidos para a implementação do projeto.

A ideia é, basicamente, criar uma TO-DO list com sistema de login de usuários.

### Tecnologias
- Frontend: Blazor Web App (com uso OBRIGATÓRIO de Bootstrap)
- Backend: .NET Web API 
- Banco de Dados: Microsoft SQL Server. 
- OBS: disponibilizar como repositório público no GitHub

### Entrega

Anexar um vídeo mostrando o funcionamento do sistema

### Funcionalidades do sistema

1. Sistema de login de usuários

2. CRUD de tarefas

#### 1. Sistema de login de usuários

Deverão existir 4 páginas referentes à usuários
1. Página de login
2. Página de cadastro
3. Página de visualização/deleção da conta do usuário

O banco de dados deverá, OBRIGATÓRIAMENTE, ter o seguinte usuário cadastrado:
- Login: admin
- Senha: Admin@ICAD!

Um usuário deslogado NÃO poderá acessar as páginas que não sejam de login/cadastro
-  Nota: é aconselhável que essa verificação seja feita a cada requisição

Utilizar algum sistema de login que já tenha criptografia de senhas embutido

#### 2. CRUD de tarefas

Deverá haver uma página para listagem de tarefas, onde são obrigatórios:
- Um checkbox para cada tarefa 
- Um accordion para cada tarefa (elemento Bootstrap) - Ao ser clicado, deverá exibir outras informações da tarefa

As informações a serem listadas inicialmente em uma tarefa são título e data. O restante deve aparecer apenas com o clique no accordion

Para cada tarefa deverão haver 2 botões
- Edição de tarefa
- Deleção de tarefa

Informações da tarefa
- Título | String
- Descrição | String
- Data de entrega | Date
- Responsável (Referência à 1 USUÁRIO da tabela de usuários)
- Dificuldade (String) -> Deve ser mostrada como tag ("FÁCIL", "MÉDIA", "DIFÍCIL")
- Concluída (Boolean) -> Não precisa aparecer no cadastro | Padrão = false

Observações sobre as informações da tarefa
- A data de entrega não pode ser anterior à data atual
- Uma tarefa só pode ter 1 responsável
- O responsável da tarefa NÃO necessariamente será seu criador

O botão de logout deverá fazer o usuário ser redirecionado à página de login

Criar filtro na página de listagem de tarefas, permitindo pesquisar PRINCIPALMENTE pelo nome

APENAS o admin pode deletar uma tarefa. 
O responsável pode APENAS visualizar/editar a tarefa.
Um usuário comum (não-responsável e não-admin) só pode visualizar a tarefa e atribuir-se como responsável caso ela não tenha nenhum

A dificuldade deve ser encarada como um enumerado. Não podem ser criados novos enumerados nem editados os já existentes.

### Outros

Em todo o momento deverá haver uma navbar com logo/nome do site e alguns botões para navegar entre as páginas, são eles: "Lista de Tarefas", "Adicionar Nova Tarefa" e "Logout"
- ATENÇÃO: "Logout" deve ser vermelho

Tanto os dados dos usuários quanto as tarefas deverão ser armazenadas no banco de dados Microsoft SQL Server

### Perguntas

1. O projeto deverá ser colocado no ar ou devo criar um arquivo indicando como rodar o projeto? Nesse segundo caso, eu teria que manter deixar o banco de dados exposto no GitHub (acho eu).

R: O projeto não precisa ser colocado no ar

2. O criador da tarefa deverá ser automaticamente o responsável por ela? Uma tarefa pode não ter responsável? Uma tarefa pode ter + de 1 responsável?

R: O criador da tarefa não é necessariamente o responsável, e não, uma tarefa não pode ter mais de um responsável

3. É necesário implementar recuperação de senha; mudança de email, nome de usuário e senha; e deleção de conta?

Supondo que seja necessário implementar a deleção de conta: qual deve ser o comportamento quando um usuário deletar sua conta
- Suas tarefas também serão apagadas?
- Devo ativamente apagar os dados do usuário do banco de dados ou setar uma flag que indica que ele apagou sua conta é o mais adequado?

R: Não é necessário implementar recuperação de senha, nem edição de conta

4. As ações como adicionar/editar uma tarefa deverão ser novas requisições (consequentemente com URLs diferentes) ou é esperado que os campos se tornem editáveis ao clicar no botão, permanecendo no mesmo URL?

R: No back são novas URLs, no front também são diferentes, porém podem ser modais se quiser

5. Qualquer usuário pode marcar o checkbox da tarefa, editar informações da tarefa e deletá-la ou apenas o responsável?

R: Pelo que me lembro o admin pode tudo, mas o responsável da tarefa não pode excluir ima tarefa assim não, acho que só editar

6. No enunciado, é dito que um accordion deverá acionar a exibição de outras informações tarefa. Quais informações devem ser mostradas inicialmente e quais seriam as outras?

R: As unicas informações a serem mostradas são título da tarefa, data e o checkbox pra marcar ela como concluída, de resto aparece nos detalhes

7. Os dados dos usuários podem ser salvos simplesmente como strings no banco de dados ou devem ser tratados com criptografia?

R: Pode criptografar a senha, é ideal

8. O que significa a dificuldade ser mostrada como tag? Significa que serão opções pré-estabelecidas que não podem ser alteradas pelo usuário? Exemplo: "Fácil", "Média" e "Difícil"?

R: Exatamente, usuário não pode alterar elas

9. Quanto é dito que "O login não deve ser fixo, e sim checar se o usuário está registrado no banco de dados (tabela User).". 
- Isso especifica que o usuário deverá ser checado no banco de dados a cada requisição e não apenas no "ponto de entrada" do website?

R: Não, basta checar ao logar, não precisa ficar checando o tempo todo