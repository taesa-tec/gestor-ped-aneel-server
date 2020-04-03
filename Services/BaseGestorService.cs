﻿using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using APIGestor.Authorizations;
using APIGestor.Data;
using APIGestor.Models.Projetos;
using APIGestor.Security;
using APIGestor.Services.Projetos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace APIGestor.Services {
    public class BaseGestorService {

        protected IAuthorizationService authorization;
        public readonly GestorDbContext _context;
        public readonly LogService LogService;
        public BaseGestorService( GestorDbContext context, IAuthorizationService authorizationService, LogService logService ) {
            this.authorization = authorizationService;
            this._context = context;
            this.LogService = logService;

        }

        public bool UserProjectCan( int projeto_id, ClaimsPrincipal User, ProjectAccessRequirement projectAccess = null ) {
            var user_role = User.FindFirst(ClaimTypes.Role).Value;

            if(user_role == Roles.ROLE_ADMIN_GESTOR) {
                return true;
            }

            var user_id = User.FindFirst(JwtRegisteredClaimNames.Jti).Value.ToString();

            var userProjeto = this.UserProjeto(user_id, projeto_id);

            return authorization.AuthorizeAsync(User, userProjeto, projectAccess != null ? projectAccess : ProjectPermissions.Leitura).Result.Succeeded;
        }

        public UserProjeto UserProjeto( string user_id, int projeto_id ) {
            return _context.UserProjetos
                .Include("CatalogUserPermissao")
                .Where(up => up.UserId == user_id && up.ProjetoId == projeto_id)
                .FirstOrDefault();
        }
    }


}