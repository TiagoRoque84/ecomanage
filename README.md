# EcoManage Web 🌱

Este é o **EcoManage Web**, um sistema completo e responsivo para gestão integrada de resíduos sólidos urbanos e industriais (aterros sanitários). Desenvolvido como parte do **Projeto Integrado Multidisciplinar III (PIM III)** do Curso de Análise e Desenvolvimento de Sistemas da UNIP.

## 🚀 Como rodar o projeto na sua máquina

O sistema foi desenvolvido utilizando **ASP.NET Core (C#)** e banco de dados **SQLite**, o que significa que ele é extremamente fácil de rodar em qualquer computador, sem a necessidade de configurar servidores de banco de dados externos.

### 1. Pré-requisitos
Certifique-se de que você possui o **.NET SDK** instalado na sua máquina (versão 8.0 ou superior).
- [Baixar .NET SDK](https://dotnet.microsoft.com/download)

### 2. Clonando e Executando
Abra o seu terminal (Prompt de Comando ou PowerShell) e siga os passos abaixo:

```bash
# Clone este repositório
git clone https://github.com/TiagoRoque84/ecomanage.git

# Entre na pasta do projeto
cd ecomanage

# Execute o projeto
dotnet run
```

### 3. Banco de Dados Automático (Magia pura! ✨)
Você não precisa se preocupar em criar ou configurar o banco de dados. 
No momento em que você der o comando `dotnet run` pela primeira vez, o sistema irá:
1. Criar o banco de dados local `ecomanage.db` automaticamente.
2. Criar todas as tabelas (Code-First Migration).
3. Inserir os dados iniciais de configuração (Tipos de Resíduos e Capacidade).
4. Criar os **usuários padrão** para você acessar.

### 4. Acessando o Sistema
Após rodar o comando, o terminal exibirá as portas nas quais o servidor está escutando (geralmente `http://localhost:5235`). Abra essa URL no seu navegador.

Para fazer login no sistema, utilize as seguintes credenciais padrão que foram geradas automaticamente para você:

**Acesso Administrativo (Acesso Total):**
- **E-mail:** `admin@eco.com`
- **Senha:** `admin123`

**Acesso Balanceiro (Apenas Operação de Balança):**
- **E-mail:** `balanca@eco.com`
- **Senha:** `admin123`

---

## 📱 Acesso via Celular / Rede Local
O projeto já está configurado para aceitar conexões em toda a rede local (Wi-Fi). 
Para abrir o sistema no seu celular e testar o **design responsivo e o Portal do Cliente**:
1. No Windows, descubra o seu IP local digitando `ipconfig` no terminal.
2. Com o `dotnet run` rodando, abra o navegador do celular conectado no mesmo Wi-Fi.
3. Digite `http://SEU_IP:5235` (Exemplo: `http://192.168.1.15:5235`).
4. *Nota: Se a página não carregar, lembre-se de liberar a porta 5235 no Firewall do Windows e certifique-se de que a sua rede está configurada como "Particular".*

## 🛠️ Principais Tecnologias Utilizadas
- **Backend:** C#, ASP.NET Core MVC, Entity Framework Core.
- **Banco de Dados:** SQLite (Fácil portabilidade).
- **Frontend:** HTML5, CSS3, Bootstrap 5.3 (Responsivo), JavaScript.
- **Gerador de PDF:** iText7.
- **Acessibilidade:** Suíte VLibras integrada.

<br>
<p align="center">
Desenvolvido por <strong>Tiago Roque</strong> - PIM III (2026)
</p>
