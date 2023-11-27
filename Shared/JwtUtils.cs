using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using Server1001.Models;

namespace Server1001.Shared;

public interface IJwtUtils
{
    public string GenerateToken(User user);
    public Guid? ValidateToken(string token);
}

public class JwtUtils : IJwtUtils
{
    private ILogger<IJwtUtils> _logger;
    private static string _secret = "";

    public JwtUtils(ILogger<IJwtUtils> logger, ICustomConfiguration config)
    {
        _logger = logger;
        _secret = config.AuthTokenSecret;
    }

    public string GenerateToken(User user)
    {
        try {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
                Expires = DateTime.Now.AddYears(5),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception e)
        {
            _logger.LogError(Events.JwtError, e, "Big error in ValidateToken");
            return string.Empty;
        }        
    }

    public Guid? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token)) 
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secret);
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = Guid.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

            return userId;
        }
        catch (Exception e)
        {
            _logger.LogError(Events.JwtError, e, "Big error in ValidateToken");
            return null;
        }
    }
}