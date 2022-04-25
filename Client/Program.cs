using System;
using Cryptography;
using Cryptography.EllipticCurveCryptography;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Buffers;
using System.Text.Json;
using System.Text;
using System.Net;
using Cryptography.Networking;
using System.Numerics;

namespace APIClient
{
    public enum Method
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    class SignedAPIClient : APIClient
    {
        string public_key;

        public SignedAPIClient(Socket socket, string public_key) : base(socket)
        {
            this.public_key = public_key;
        }

        public SignedAPIClient(string remote_address, string public_key) : base(remote_address)
        {
            this.public_key = public_key;
        }

        protected override Span<byte> recv(SecureSocket socket)
        {
            return socket.secureRecvSigned(public_key);
        }
    }

    class APIClient
    {
        private SecureSocket socket_wrapper;

        public APIClient(Socket socket)
        {
            socket_wrapper = new SecureSocket(socket);
        }

        public APIClient(string remote_address)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint end_point = IPEndPoint.Parse(remote_address);

            socket.Connect(end_point);
            socket_wrapper = new SecureSocket(socket);
        }

        public Dictionary<string, string>? request(Method method, string uri, Dictionary<string, string>? body)
        {
            string json_body = JsonSerializer.Serialize(body);

            Dictionary<string, string>? headers = new Dictionary<string, string>();
            headers["method"] = method.ToString();
            headers["URI"] = uri;
            headers["body"] = json_body;

            string json_headers = JsonSerializer.Serialize(headers);

            send(socket_wrapper, Encoding.UTF8.GetBytes(json_headers));
            Span<byte> response_json = recv(socket_wrapper);

            Dictionary<string, string>? response = JsonSerializer.Deserialize<Dictionary<string, string>>(response_json);
            if (response?["response_code"] != "200")
            {
                throw new Exception(response?["error_message"]);
            }

            string? response_body_json = response?["response_body"];
            Dictionary<string, string>? response_body = default;
            if (response_body_json != null)
            {
                response_body = JsonSerializer.Deserialize<Dictionary<string, string>>(response_body_json);
            }

            return response_body;
        }

        private void send(SecureSocket socket, Span<byte> data)
        {
            socket.secureSend(data);
        }

        protected virtual Span<byte> recv(SecureSocket socket)
        {
            return socket.secureRecv();
        }
    }

    class SecureSpeakClient
    {
        const string server_public_key = "40f0ffb9a97371d0fbc64b40770fdd97e8cb80e1,08370abd6f697387ca1b10c7b676d12354ec7a840";

        BigInteger identity_key_private = 0;
        BigInteger signed_prekey_private = 0;

        string username = "";
        string session_token = "";

        SignedAPIClient client;

        public SecureSpeakClient(string address)
        {
            client = new SignedAPIClient(address, server_public_key);
        }

        public void signUp(string username, string password)
        {
            KeyPair idkey = new KeyPair(Curves.microsoft_160);
            identity_key_private = idkey.private_component;

            KeyPair signedprekey = new KeyPair(Curves.microsoft_160);
            signed_prekey_private = signedprekey.private_component;

            ECC ecc = new ECC(Curves.microsoft_160);
            string signature = ecc.generateDSAsignature(signedprekey.public_component_string, idkey.private_component).signature;
            
            var request_body = new Dictionary<string, string>()
            {
                { "username", username },
                { "password", password },
                { "idkey", idkey.public_component_string },
                { "signedprekey", signedprekey.public_component_string },
                { "prekeysignature", signature }
            };

            client.request(Method.POST, "api/accounts/signup", request_body);
        }
    }

    class Program
    {

        static void Main()
        {
            string public_key = "40f0ffb9a97371d0fbc64b40770fdd97e8cb80e1,08370abd6f697387ca1b10c7b676d12354ec7a840";
            
            

            Console.WriteLine("Connected");

            var request_body = new Dictionary<string, string>()
            {
                { "username", "Bob" },
                { "password", "BobPass" },
                { "idkey", "BobIDkey" },
                { "signedprekey", "BobSignedPreKey" },
                { "prekeysignature", "BobSignature" }
            };

            client.request(Method.POST, "api/accounts/signup", request_body);


            Console.WriteLine();
            Console.WriteLine("End");
        }
    }
}