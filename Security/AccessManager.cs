﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using APIGestor.Models;
using APIGestor.Models.Projetos;
using APIGestor.Requests;
using APIGestor.Services;
using APIGestor.Views.Email;

namespace APIGestor.Security
{
    public class AccessManager
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private SigningConfigurations _signingConfigurations;
        private TokenConfigurations _tokenConfigurations;
        private IWebHostEnvironment _hostingEnvironment;
        private MailService _mailService;
        protected SendGridService SendGridService;

        public AccessManager(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            SigningConfigurations signingConfigurations,
            IWebHostEnvironment hostingEnvironment,
            TokenConfigurations tokenConfigurations,
            MailService mailService,
            SendGridService sendGridService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _signingConfigurations = signingConfigurations;
            _hostingEnvironment = hostingEnvironment;
            _tokenConfigurations = tokenConfigurations;
            _mailService = mailService;
            SendGridService = sendGridService;
        }

        public bool ValidateCredentials(Login user)
        {
            bool credenciaisValidas = false;
            if (user != null && !String.IsNullOrWhiteSpace(user.Email))
            {
                // Verifica a existência do usuário nas tabelas do
                // ASP.NET Core Identity
                var userIdentity = _userManager
                    .FindByEmailAsync(user.Email).Result;
                if (userIdentity != null)
                {
                    // Efetua o login com base no Id do usuário e sua senha
                    var resultadoLogin = _signInManager
                        .CheckPasswordSignInAsync(userIdentity, user.Password, false)
                        .Result;
                    if (resultadoLogin.Succeeded)
                    {
                        // Verifica se o usuário em questão possui
                        // a role Acesso-APIGestor
                        // credenciaisValidas = _userManager.IsInRoleAsync(
                        //     userIdentity, Roles.ROLE_ADMIN_GESTOR).Result;
                        //Verifica se o usuário está Ativo
                        credenciaisValidas = (userIdentity.Status > 0) ? true : false;
                    }
                }
            }

            return credenciaisValidas;
        }

        public Token GenerateToken(Login user)
        {
            var userIdentity = _userManager
                .FindByEmailAsync(user.Email).Result;

            ClaimsIdentity identity = new ClaimsIdentity(
                new GenericIdentity(user.Email, "Login"),
                new[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti, userIdentity.Id),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.Email),
                    new Claim(ClaimTypes.Role, userIdentity.Role),
                }
            );

            DateTime dataCriacao = DateTime.Now;
            DateTime dataExpiracao = dataCriacao +
                                     TimeSpan.FromSeconds(_tokenConfigurations.Seconds);

            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _tokenConfigurations.Issuer,
                Audience = _tokenConfigurations.Audience,
                SigningCredentials = _signingConfigurations.SigningCredentials,
                Subject = identity,
                NotBefore = dataCriacao,
                Expires = dataExpiracao
            });
            var token = handler.WriteToken(securityToken);

            return new Token()
            {
                Authenticated = true,
                Created = dataCriacao.ToString("yyyy-MM-dd HH:mm:ss"),
                Expiration = dataExpiracao.ToString("yyyy-MM-dd HH:mm:ss"),
                AccessToken = token,
                Message = "OK"
            };
        }

        public Resultado RecuperarSenha(Login user)
        {
            Resultado resultado = new Resultado();
            resultado.Acao = "Recuperação de senha de User";

            if (String.IsNullOrWhiteSpace(user.Email))
            {
                resultado.Inconsistencias.Add(
                    "Preencha o E-mail do Usuário");
                return resultado;
            }

            try
            {
                SendRecoverAccountEmail(user.Email).Wait(10000);
            }
            catch (Exception e)
            {
                resultado.Inconsistencias.Add(e.Message);
            }

            return resultado;
        }

        public Resultado NovaSenha(User user)
        {
            Resultado resultado = new Resultado();
            resultado.Acao = "Recuperação de senha de User";

            if (String.IsNullOrWhiteSpace(user.Email))
            {
                resultado.Inconsistencias.Add(
                    "Preencha o E-mail do Usuário");
            }

            if (String.IsNullOrWhiteSpace(user.NewPassword))
            {
                resultado.Inconsistencias.Add(
                    "Preencha a nova senha do Usuário");
            }

            var User = _userManager.Users
                .Where(u => u.Email == user.Email)
                .FirstOrDefault();
            if (User == null)
            {
                resultado.Inconsistencias.Add(
                    "usuário não localizado");
            }

            if (resultado.Inconsistencias.Count == 0)
            {
                var result = _userManager.ResetPasswordAsync(User, user.ResetToken, user.NewPassword).Result;
                if (result.Errors.Count() > 0)
                {
                    foreach (var error in result.Errors)
                    {
                        resultado.Inconsistencias.Add(error.Description);
                    }
                }
            }

            return resultado;
        }

        public async Task SendRecoverAccountEmail(string email, bool newAccount = false,
            string subject = "Redefinição de Senha - Gerenciador P&D Taesa")
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new Exception("Email não encontrado");
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await SendGridService.Send(email, subject, newAccount ? "Email/RegisterAccount" : "Email/RecoverAccount",
                new RecoverAccount()
                {
                    Email = email,
                    Token = token
                });
        }
    }
}