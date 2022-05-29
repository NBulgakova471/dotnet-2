using ChatService.Exceptions;
using ChatService.Repository;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ChatService
{
    public class Greet��Service : ChatGreeter.ChatGreeterBase
    {
        private readonly ILogger<Greet��Service> _logger;
        private readonly IRoomRepository _repository;
        public Greet��Service(ILogger<Greet��Service> logger, IRoomRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public override async Task Create(IAsyncStreamReader<Message> requestStream, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            if (!await requestStream.MoveNext()) 
                return;
            try
            {
                _repository.AddRoom(requestStream.Current.RoomName);
                _repository.Join(requestStream.Current.RoomName, requestStream.Current.UserName, responseStream);
                await _repository.WriteFileAsync();
                await responseStream.WriteAsync(new Message { Text = requestStream.Current.Text });
                await requestStream.MoveNext();
            }
            catch(NameIsUseException)
            {
                await responseStream.WriteAsync(new Message { Text = "������ ������� ����������!" });
            }
        }

        public override async Task Join(IAsyncStreamReader<Message> requestStream, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            if (!await requestStream.MoveNext()) 
                return;
            if (_repository.FindRoom(requestStream.Current.RoomName) == null)
            {
                await _repository.ReadFileAsync(requestStream.Current.RoomName);

                _repository.Join(requestStream.Current.RoomName, requestStream.Current.UserName, responseStream);


                await responseStream.WriteAsync(new Message { Text = "Connection success" });
                await _repository.BroadcastMessage(
                    new Message { Text = $"{requestStream.Current.UserName} connected" }, 
                    requestStream.Current.RoomName, 
                    requestStream.Current.UserName
                    );
            }
            else
            {
                await responseStream.WriteAsync(new Message { Text = "No connection!" });
                return;
            }
            while (await requestStream.MoveNext())
            {
                var currentMessage = requestStream.Current;
                await _repository.BroadcastMessage(requestStream.Current, requestStream.Current.RoomName, requestStream.Current.UserName);
            }
            await _repository.WriteFileAsync();
            _repository.Disconnect(requestStream.Current.RoomName, requestStream.Current.UserName);
        }
    }
}
