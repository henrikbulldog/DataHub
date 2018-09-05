using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RepositoryFramework.Interfaces;

namespace DataHub.Controllers
{
    [Route("[controller]")]
    public class MessagesController : Controller
    {
        private IRepository<Models.MessageInfo> messsagesRepository;

        public MessagesController(
            IRepository<Models.MessageInfo> messsagesRepository)
        {
            this.messsagesRepository = messsagesRepository;
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> Get([FromRoute]string id)
        {
            var message = await messsagesRepository.GetByIdAsync(id);
            if (message == null)
            {
                return NotFound($"No data found for message id {id}");
            }

            return Ok(message);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody]Models.MessageRequest messageRequest)
        {
            if (messageRequest == null)
            {
                return BadRequest("No message data");
            }

            var id = Guid.NewGuid().ToString();
            var messageInfo = new Models.MessageInfo
            {
                Id = id,
                Source = messageRequest.Source,
                Entity = messageRequest.Entity,
                Time = messageRequest.Time,
                Payload = messageRequest.Payload,
                Uri = this.BuildLink($"/messages/{id}")
            };
            await messsagesRepository.CreateAsync(messageInfo);
            return Ok(messageInfo);
        }
    }
}
