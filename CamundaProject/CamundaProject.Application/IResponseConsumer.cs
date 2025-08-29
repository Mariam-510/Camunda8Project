using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Application
{
    public interface IResponseConsumer : IConsumer<string, string> { }

}
