using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using B2CSample.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace B2CSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        [Route("signup")]
        [HttpPost]
        public async Task<ActionResult<B2CResponseContent>> SignUp()
        {
            string input;

            // If no data came in, then return
            if (Request.Body == null) throw new Exception();

            //read data from body
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                input = await reader.ReadToEndAsync();
            }


            // Check the input content value
            if (string.IsNullOrEmpty(input))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, new B2CResponseContent("Request content is empty", HttpStatusCode.BadRequest));
            }

            // Convert the input string into an InputClaimsModel object
            InputClaimsModel inputClaims = JsonConvert.DeserializeObject(input, typeof(InputClaimsModel)) as InputClaimsModel;

            if (inputClaims == null)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, new B2CResponseContent("Can not deserialize input claims", HttpStatusCode.BadRequest));
            }

            // Run an input validation
            if (inputClaims.FirstName.ToLower() == "test")
            {
                return StatusCode((int)HttpStatusCode.BadRequest, new B2CResponseContent("Test name is not valid, please provide a valid name", HttpStatusCode.BadRequest));
            }

            // Create an output claims object and set the loyalty number with a random value
            OutputClaimsModel outputClaims = new OutputClaimsModel();
            outputClaims.LoyaltyNumber = new Random().Next(100, 1000).ToString();

            // Return the output claim(s)
            return Ok(outputClaims);
        }
        //simulates validating credentials and getting user data from an external store outside of azure
        [Route("signin")]
        [HttpPost]
        public ActionResult<SignInResponseContent> SignIn([FromBody]SignInInput signInInput)
        {
            if (signInInput == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent("Can not deserialize input claims", HttpStatusCode.BadRequest));
            }
            if (signInInput.Email.ToLower().Contains("test2"))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, new B2CResponseContent("Test name is not valid, please provide a valid name", HttpStatusCode.BadRequest));
            }
            SignInResponseContent outputClaims = new SignInResponseContent()
            {
                DisplayName = "User Test 1",
                Email = signInInput.Email,
                Firstname = "User1",
                Lastname = "Test1",
                ValidCreds = "true"
            };
            if (signInInput.Email.ToLower().Contains("invalidcreds") || signInInput.Password.ToLower().Contains("wrong"))
            {
                outputClaims.ValidCreds = "false";
            }
            return Ok(outputClaims);
        }
    }
}