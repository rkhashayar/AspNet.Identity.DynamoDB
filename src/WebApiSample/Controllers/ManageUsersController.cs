using AspNet.Identity.DynamoDB.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiSample.ViewModels;

namespace WebApiSample.Controllers;

[Route("identity/users")]
public class ManageUsersController(UserManager<DynamoDbIdentityUser> userManager) : ControllerBase
{
    // GET api/values
    [HttpPost]
    [Route("register")]
    public async Task<IdentityResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        return await userManager.CreateAsync(new DynamoDbIdentityUser(model.Email), model.Password);
    }

    // GET api/values/5
    [HttpGet("{id}")]
    public string Get(int id)
    {
        return "value";
    }

    // POST api/values
    [HttpPost]
    public void Post([FromBody]string value)
    {
    }

    // PUT api/values/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody]string value)
    {
    }

    // DELETE api/values/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}