using ImageUpload.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ImageUpload.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerImageController : ControllerBase
    {
        private readonly CustomerImageService _customerImageService;

        public CustomerImageController(CustomerImageService customerImageService)
        {
            _customerImageService = customerImageService;
        }


        [HttpPost("customers/{id:guid}/image")]
        public async Task<IActionResult> Upload([FromRoute] Guid id,
            [FromForm(Name ="Data")] IFormFile file)
        {
            var response =await _customerImageService.UploadImageAsync(id, file);
            if(response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return Ok(response);
            }
            return BadRequest();
        }

        [HttpGet("customers/{id:guid/image")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            var response = await _customerImageService.GetImageAsyc(id);
            if(response is null)
            {
                return NotFound();
            }

            return File(response.ResponseStream, response.Headers.ContentType);
        }

        [HttpDelete("customers/{id:guid}/image")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var response = await _customerImageService.DeleteImageAsync(id);
            return response.HttpStatusCode switch
            {
                HttpStatusCode.NoContent => Ok(),
                HttpStatusCode.NotFound => NotFound(),
                _ => BadRequest()
            };
        }
    }
}
