using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ThirdPartyApis;
using Contracts;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebServer.Controllers
{
    [Route("api/[controller]")]
    public class ParkingController : Controller
    {
	private IPassToParkIt ParkingService;
	private IGmailApiService gmailService;

	public ParkingController(IPassToParkIt parkingService, IGmailApiService gmailService)
	{
	    this.ParkingService = parkingService;
	    this.gmailService = gmailService;
	}

        // GET: api/parking
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/parking/5
        [HttpGet("{id}")]
        public JsonResult Get(int id)
        {
            if (id != 750)
            {
                return new JsonResult(String.Empty);
            }

	    var json = System.IO.File.ReadAllText("ParkingInfo.json");

	    ParkingInformation parkInfo = JsonSerializer.Deserialize<ParkingInformation>(json);

            // Sign into parking app
            // Save Screenshot
            // Send email to recipient
	    var result = ParkingService.ParkMyCar(parkInfo);
	    // Send email
	    if (result.Success)
	    {
		var emailInfoJson = System.IO.File.ReadAllText("EmailInfo.json");
		var emailInfo = JsonSerializer.Deserialize<EmailInformation>(emailInfoJson);
		var emailResult = gmailService.SendEmail(result.ParkingPassLocation, emailInfo);
		return new JsonResult(emailResult);
	    }
	    else
	    {
		return new JsonResult(result);
	    }
        }

	[HttpGet]
	[Route("smoketest")]
	public IEnumerable<string> SmokeTest()
	{
	    ParkingService.ChromedriverSmokeTest();

	    return new string[] { "radical" };
	}
    }
}
