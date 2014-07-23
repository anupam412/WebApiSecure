﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WebApiSecure.Interfaces;

namespace WebApiSecure.Handlers
{
    public class AuthHandler:DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            HttpStatusCode statusCode = HttpStatusCode.BadRequest;
            AuthenticationHeaderValue authValue = request.Headers.Authorization;

            if (authValue != null && !String.IsNullOrWhiteSpace(authValue.Parameter))
            {
                try
                {
                    var validationService = request.GetDependencyScope().GetService(typeof(IValidateToken)) as IValidateToken;
                    if (authValue.Scheme == "Basic" && request.RequestUri.PathAndQuery.Contains(validationService.AllowedTokenRoute))
                        return base.SendAsync(request, cancellationToken);
                    else if (authValue.Scheme == "Bearer")
                    {
                        var token = authValue.Parameter;
                        var claimsPrincipal = validationService.ValidateToken(token);
                        Thread.CurrentPrincipal = claimsPrincipal;
                        HttpContext.Current.User = claimsPrincipal;
                        return base.SendAsync(request, cancellationToken);
                    }
                }
                catch (SecurityTokenValidationException)
                { statusCode = HttpStatusCode.Unauthorized; }
                catch (Exception)
                { statusCode = HttpStatusCode.InternalServerError; }
            }
            return Task<HttpResponseMessage>.Factory.StartNew(() => new HttpResponseMessage(statusCode));
        }     
    }
}