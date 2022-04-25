using System.Numerics;
using MySql.Data.MySqlClient;

namespace APIcontroller
{
    class Program
    {
        static MySqlConnection db = new MySqlConnection("server=localhost;userid=DBworker;password=MysqlPasswordTest;database=SecureSpeak");

        static void Main(string[] args)
        {
            db.Open();
            BigInteger private_key = BigInteger.Parse("90872456498734350987250659872549646709847697687433539873946766");

            SignedController controller = new SignedController("127.0.0.1:8064", "api", private_key);
            controller.start();
        }

        static bool validateToken(string token, string username)
        {
            using var select_token_cmd = new MySqlCommand("SELECT sessions.token FROM sessions INNER JOIN userdata ON sessions.userID=userdata.userID WHERE userdata.username=@username;", db);
            select_token_cmd.Parameters.AddWithValue("@username", username);
            select_token_cmd.Prepare();

            string? db_token = select_token_cmd.ExecuteScalar().ToString();
            return db_token == token;
        }

        [ResourceGroup("/messaging")]
        class Messaging
        {
            [Resource("/uploadPrekey", Method.POST)]
            public static APIResponse uploadPrekeys(APIRequest request)
            {
                if (!validateToken(request.request_body["token"], request.request_body["username"]))
                {
                    return new APIResponse(error_message: "token is not valid", response_code: "401");
                }

                using var cmd = new MySqlCommand("INSERT INTO prekeys (prekey, userID) VALUES (@prekey, (SELECT userID FROM userdata WHERE userdata.username=@username));", db);
                cmd.Parameters.AddWithValue("@prekey", request.request_body["prekey"]);
                cmd.Parameters.AddWithValue("@username", request.request_body["username"]);

                cmd.Prepare();
                cmd.ExecuteNonQuery();

                return new APIResponse();
            }

            [Resource("/uploadSignedPrekey", Method.POST)]
            public static APIResponse uploadSignedPrekey(APIRequest request)
            {
                if (!validateToken(request.request_body["token"], request.request_body["username"]))
                {
                    return new APIResponse(error_message: "token is not valid", response_code: "401");
                }

                using var cmd = new MySqlCommand("UPDATE signedPreKey SET signedPreKey=@signedPK, preKeySignature=@PKSignature WHERE userID=(SELECT userID FROM userdata WHERE userdata.username=@username);", db);
                cmd.Parameters.AddWithValue("@signedPK", request.request_body["signedPK"]);
                cmd.Parameters.AddWithValue("@PKSignature", request.request_body["PKSignature"]);
                cmd.Parameters.AddWithValue("@username", request.request_body["username"]);

                cmd.Prepare();
                cmd.ExecuteNonQuery();

                return new APIResponse();
            }

            [Resource("/E2Ehandshake", Method.GET)]
            public static APIResponse E2Ehandshake(APIRequest request)
            {
                if (validateToken(request.request_body["token"], request.request_body["username"]))
                {
                    return new APIResponse(error_message: "token is not valid", response_code: "401");
                }

                using var cmd = new MySqlCommand("SELECT preKeys.preKeyID, preKeys.preKey, signedPrekeys.signedPreKey, signedPrekeys.preKeySignature, userdata.identityKey FROM preKeys INNER JOIN signedPrekeys ON preKeys.userID=signedPrekeys.userID INNER JOIN userdata ON preKeys.userID=userdata.userID WHERE userdata.username = @recieverUsername;", db);
                cmd.Parameters.AddWithValue("@recieverUsername", request.request_body["recieverUsername"]);
                cmd.Prepare();

                Dictionary<string, string> response = new Dictionary<string, string>();

                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    for (int i = 0; i < 5; i++)
                    {
                        response[reader.GetName(i)] = (string)reader.GetValue(i);
                    }
                }

                return new APIResponse(response);
            }

            [Resource("/sendMessage", Method.POST)]
            public static APIResponse sendMessage(APIRequest request)
            {
                if (validateToken(request.request_body["token"], request.request_body["username"]))
                {
                    return new APIResponse(error_message: "token is not valid", response_code: "401");
                }

                string preKeyID = "";
                if (request.request_body.ContainsKey("preKeyID"))
                {
                    preKeyID = request.request_body["preKeyID"];
                }

                using var cmd = new MySqlCommand("INSERT INTO messages (SenderID, ReciverID, MessageData, EphemeralKey, preKeyID) VALUES ((SELECT userID FROM userdata WHERE username=@username), (SELECT userID FROM userdata WHERE username=@recieverUsername), @messageData, @ephemeralKey, @preKeyID);", db);
                cmd.Parameters.AddWithValue("@recieverUsername", request.request_body["username"]);
                cmd.Parameters.AddWithValue("@recieverUsername", request.request_body["recieverUsername"]);
                cmd.Parameters.AddWithValue("@messageData", request.request_body["messageData"]);
                cmd.Parameters.AddWithValue("@ephemeralKey", request.request_body["ephemeralKey"]);
                cmd.Parameters.AddWithValue("@preKeyID", preKeyID);
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                return new APIResponse();
            }
        }

        [ResourceGroup("/accounts")]
        class Accounts
        {
            [Resource("/login", Method.POST)]
            public static APIResponse login(APIRequest request)
            {
                var username = request.request_body["username"];
                var password = request.request_body["password"];

                Dictionary<string, string> response = new Dictionary<string, string>()
                    {
                        {"authenticated", "false" },
                    };

                using var cmd = new MySqlCommand("SELECT password, userID FROM userdata WHERE username=@username;", db);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Prepare();

                string? db_password;
                int userID;
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    db_password = reader.GetString(0);
                    userID = reader.GetInt32(1);
                }

                //testing only (no hashing)
                if (db_password == password)
                {
                    string token = Guid.NewGuid().ToString();

                    using var updatecmd = new MySqlCommand("UPDATE sessions SET token=@token WHERE userID=@userID;", db);
                    updatecmd.Parameters.AddWithValue("@token", token);
                    updatecmd.Parameters.AddWithValue("@userID", userID);
                    updatecmd.Prepare();

                    updatecmd.ExecuteNonQuery();

                    response["authenticated"] = "true";
                    response["token"] = token;
                }

                return new APIResponse(response);
            }

            [Resource("/signup", Method.POST)]
            public static APIResponse signup(APIRequest request)
            {
                var username = request.request_body["username"];
                var password = request.request_body["password"];
                var idkey = request.request_body["idkey"];
                var signedprekey = request.request_body["signedprekey"];
                var signature = request.request_body["prekeysignature"];

                using (var cmd = new MySqlCommand("INSERT INTO userdata (username, password, identityKey) VALUES (@username, @password, @idkey);", db))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.Parameters.AddWithValue("@idkey", idkey);
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new MySqlCommand("INSERT INTO signedPrekeys (signedPreKey, preKeySignature, userID) VALUES (@signedprekey, @prekeysignature, (SELECT userID FROM userdata WHERE username=@username));", db))
                {
                    cmd.Parameters.AddWithValue("@signedprekey", signedprekey);
                    cmd.Parameters.AddWithValue("@prekeysignature", signature);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }

                string token = Guid.NewGuid().ToString();
                using (var cmd = new MySqlCommand("INSERT INTO sessions (token, userID) VALUES (@token, (SELECT userID FROM userdata WHERE username=@username));", db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }

                Dictionary<string, string> response = new Dictionary<string, string>()
                {
                    {"token", token }
                };

                return new APIResponse(response);
            }
        }
    }
}