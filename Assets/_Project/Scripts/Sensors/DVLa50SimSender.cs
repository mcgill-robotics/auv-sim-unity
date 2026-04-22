using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
namespace DVLJSONProtocol
{
    public enum Status : byte
    {
        //bit mask where bit 0 set 1 to is high temperature, 0 otherwise
        OK = 0,
        HighTemperature = 1 << 0 // shift 1 to the left by 0 positions to set bit 0 to 1
    }

    // from https://docs.waterlinked.com/dvl/dvl-protocol/
    [Serializable]
    public class Transducer
    {
        public int id;
        public double velocity;
        public double distance;
        public double rssi;
        public double nsd;
        public bool beam_valid;
    }
    [Serializable]
    public class VelocityReport
    {
        public long time;
        public double vx;
        public double vy;
        public double vz;
        public double fom;
        public double[][] covariance;
        public double altitude;
        public Transducer[] transducers;
        public bool velocity_valid;
        public byte status;
        public long time_of_validity;
        public long time_of_transmission;
        public string format;
        public string type;
    }

    [Serializable]
    public class DeadReckoningReport
    {
        public long ts;
        public double x;
        public double y;
        public double z;
        public double std;
        public double roll;
        public double pitch;
        public double yaw;
        public string type;
        public byte status;
        public string format;
    }

    [Serializable]
    public class Command
    {
        public string command;
        public Dictionary<string, object> parameters;
    }

    [Serializable]
    public class CommandResponse
    {
        public string response_to;
        public bool success;
        public string error_message;
        public Dictionary<string, object> result;
        public string format;
        public string type;

        // Factory method to create a default response with success=true and no error message, we could also add the defaults to the parameters about but that may lead to unpredictable behaviour if we forget to set something for a specific command response, this way we ensure all responses have consistent default values and we can easily update the defaults in one place if needed
        public static CommandResponse GetDefault(string CommandName)
        {
            return new CommandResponse
            {
                response_to = CommandName,
                success = true,
                error_message = "",
                result = null,
                format = DVLa50SimSender.PROTOCOL_VERSION,
                type = "response"
            };
        }
    }
}
public class DVLa50SimSender : MonoBehaviour
{
    public static string PROTOCOL_VERSION = "json_v3.1";
    // server components
    TcpListener server; // server to listen for incoming connections
    TcpClient client; // connection to client
    Thread serverThread;
    bool isServerRunning = false;

    // server parameters
    [Header("Server Configuration")]
    [SerializeField] private int port = 16171; // port to listen on
    [SerializeField] private string ipAddress; // IP address to bind to
    [SerializeField] private int publishRateHz = 10; // how often to send messages (in Hz)

    // DVL components
    DVLJSONProtocol.DeadReckoningReport currentDeadReckoningReport; // store the most recent DVL dead reckoning report to send to clients when they request it
    DVLJSONProtocol.VelocityReport currentVelocityReport; // same for velocity
    long lastPublishTime = 0;
    private readonly object _reportLock = new object(); // lock to ensure reports are thread safe
    // Network Stream Lock
    private readonly object _streamWriteLock = new object();


    private void Start()
    {
        lastPublishTime = GetTimeMicroseconds(); // Initialize last publish time to current time
        // Start the server in a separate thread to avoid blocking the main Unity thread
        serverThread = new Thread(new ThreadStart(SetupServer))
        {
            IsBackground = true, // Ensure the thread will close when the application exits
            Name = "DVLa50SimSender_ServerThread"
        };
        isServerRunning = true;
        serverThread.Start();
    }


    private long GetTimeMicroseconds()
    {
        return ROSClock.GetROSTimestampNanoseconds() / 1000; // Convert nanoseconds to microseconds
    }
    // Fixed update to match physics update rate to update the stored DVL data report
    void FixedUpdate()
    {
        long currentTime = GetTimeMicroseconds();
        // TODO replace with actual data retrieval
        // Update the DVL reports with random data, lock to ensure thread safety since the server thread may be reading these reports at the same time to send to clients
        lock (_reportLock)
        {
            currentVelocityReport = new DVLJSONProtocol.VelocityReport
            {
                time = currentTime - lastPublishTime,
                vx = UnityEngine.Random.Range(-5f, 5f),
                vy = UnityEngine.Random.Range(-5f, 5f),
                vz = UnityEngine.Random.Range(-5f, 5f),
                fom = UnityEngine.Random.Range(0.1f, 1f),
                covariance = new double[][]
                    {
                        new double[] { UnityEngine.Random.Range(0.1f, 1f), 0, 0 },
                        new double[] { 0, UnityEngine.Random.Range(0.1f, 1f), 0 },
                        new double[] { 0, 0, UnityEngine.Random.Range(0.1f, 1f) }
                    },
                altitude = UnityEngine.Random.Range(0.1f, 10f),
                transducers = new List<DVLJSONProtocol.Transducer>
                    {
                        new DVLJSONProtocol.Transducer
                        {
                            id = 0,
                            velocity = UnityEngine.Random.Range(-5f, 5f),
                            distance = UnityEngine.Random.Range(0.1f, 10f),
                            rssi = UnityEngine.Random.Range(0.1f, 1f),
                            nsi = UnityEngine.Random.Range(0.1f, 1f),
                            beam_valid = true
                        },
                        new DVLJSONProtocol.Transducer
                        {
                            id = 1,
                            velocity = UnityEngine.Random.Range(-5f, 5f),
                            distance = UnityEngine.Random.Range(0.1f, 10f),
                            rssi = UnityEngine.Random.Range(0.1f, 1f),
                            nsi = UnityEngine.Random.Range(0.1f, 1f),
                            beam_valid = true
                        },
                        new DVLJSONProtocol.Transducer
                        {
                            id = 2,
                            velocity = UnityEngine.Random.Range(-5f, 5f),
                            distance = UnityEngine.Random.Range(0.1f, 10f),
                            rssi = UnityEngine.Random.Range(0.1f, 1f),
                            nsi = UnityEngine.Random.Range(0.1f, 1f),
                            beam_valid = true
                        },
                        new DVLJSONProtocol.Transducer
                        {
                            id = 3,
                            velocity = UnityEngine.Random.Range(-5f, 5f),
                            distance = UnityEngine.Random.Range(0.1f, 10f),
                            rssi = UnityEngine.Random.Range(0.1f, 1f),
                            nsi = UnityEngine.Random.Range(0.1f, 1f),
                            beam_valid = true
                        },
                    },
                velocity_valid = true,
                status = (byte)DVLJSONProtocol.Status.OK,
                time_of_validity = (long)UnityEngine.Random.Range(lastPublishTime, currentTime), // choose random time between last publish and now for when the ping reached the bottom
                time_of_transmission = currentTime,
                format = PROTOCOL_VERSION,
                type = "velocity"
            };
            currentDeadReckoningReport = new DVLJSONProtocol.DeadReckoningReport
            {
                ts = currentTime,
                x = UnityEngine.Random.Range(-10f, 10f),
                y = UnityEngine.Random.Range(-10f, 10f),
                z = UnityEngine.Random.Range(-10f, 10f),
                std = UnityEngine.Random.Range(0.1f, 1f),
                roll = UnityEngine.Random.Range(-180f, 180f),
                pitch = UnityEngine.Random.Range(-180f, 180f),
                yaw = UnityEngine.Random.Range(-180f, 180f),
                type = "position_local",
                status = (byte)DVLJSONProtocol.Status.OK,
                format = PROTOCOL_VERSION
            };
        }
    }

    async private void SetupServer()
    {
        try
        {
            // convert IP address string to IPAddress object and start the server
            IPAddress localAddr = IPAddress.Parse(ipAddress);
            server = new TcpListener(localAddr, port);
            server.Start();
            Debug.Log($"DVL Simulator started on {ipAddress}:{port}");
            Debug.Log("Waiting for a client to connect...");

            while (isServerRunning)
            {
                if (!server.Pending())
                {
                    await System.Threading.Tasks.Task.Delay(100); // wait a bit before checking for new connections to avoid busy waiting
                    continue;
                }
                // Accept incoming client connections asynchronously
                using (client = await server.AcceptTcpClientAsync())
                {

                    await HandleClient(client); // Handle the client connection (send data and read commands) until the client disconnects or an error occurs
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("DVLa50SimSender: Exception - " + e.Message);
        }
        finally
        {
            // Clean up resources
            client?.Close();
            server?.Stop();
        }
    }

    async private System.Threading.Tasks.Task HandleClient(TcpClient client)
    {
        NetworkStream stream = null;
        try
        {
            Debug.Log($"Client connected: {client.Client.RemoteEndPoint}");
            using (stream = client.GetStream())
            {
                // Spin up two independent tasks for reading and writing to stream
                System.Threading.Tasks.Task rxTask = ReadDataFromClient(stream);
                System.Threading.Tasks.Task txTask = StreamDataAsync(stream);

                // If either task completes (or fails due to disconnect), we drop the connection
                await System.Threading.Tasks.Task.WhenAll(rxTask, txTask);
                if (!client.Connected)
                {
                    Debug.Log("Client disconnected.");
                }
            }

        }
        catch (Exception e)
        {
            Debug.LogError("DVLa50SimSender: Exception in HandleClient - " + e.Message);
        }
        finally
        {
            stream?.Close();
        }
    }

    async private System.Threading.Tasks.Task StreamDataAsync(NetworkStream stream)
    {
        try
        {

            while (isServerRunning && client.Connected && stream.CanWrite)
            {
                if (currentVelocityReport == null || currentDeadReckoningReport == null)
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    continue;
                }

                DVLJSONProtocol.DeadReckoningReport localDrReport;
                DVLJSONProtocol.VelocityReport localVelReport;

                lock (_reportLock)
                {
                    localDrReport = currentDeadReckoningReport;
                    localVelReport = currentVelocityReport;
                }

                Debug.Log("Serializing DVL reports to JSON...");
                string jsonPositionReport = JsonConvert.SerializeObject(localDrReport);
                string jsonVelocityReport = JsonConvert.SerializeObject(localVelReport);

                await SendLine(stream, jsonPositionReport);
                await SendLine(stream, jsonVelocityReport);

                await System.Threading.Tasks.Task.Delay(1000 / publishRateHz);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("DVLa50SimSender: Exception in StreamDataAsync - " + e.Message);
        }
    }

    async private System.Threading.Tasks.Task SendLine(NetworkStream stream, string jsonString)
    {
        try
        {
            byte[] data = Encoding.ASCII.GetBytes(jsonString + '\n');
            await stream.WriteAsync(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DVL Server] Failed to write to stream: {e.Message}");
        }
    }
    async private System.Threading.Tasks.Task ReadDataFromClient(NetworkStream stream)
    {
        // Define the reader outside the loop
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true))
        {
            while (isServerRunning && client.Connected)
            {
                try
                {
                    string jsonCommand = await reader.ReadLineAsync();

                    if (jsonCommand == null) break; // Client disconnected gracefully

                    string response = await ProcessCommand(jsonCommand);
                    await SendLine(stream, response);
                }
                catch (IOException)
                {
                    break; // Connection lost or stream closed
                }
            }
        }
    }

    async private System.Threading.Tasks.Task<string> ProcessCommand(string jsonCommand)
    {
        try
        {
            DVLJSONProtocol.Command command = JsonConvert.DeserializeObject<DVLJSONProtocol.Command>(jsonCommand);
            Debug.Log($"Processing command: {command.command}");
            switch (command.command)
            {
                case "reset_dead_reckoning":
                    Debug.Log("Resetting dead reckoning...");
                    return ResetDeadReckoning();
                case "calibrate_gyro":
                    Debug.Log("Calibrating gyro...");
                    return CalibrateGyroResponse();
                case "trigger_ping":
                    Debug.Log("Triggering DVL ping...");
                    return TriggerPingResponse();
                case "get_config":
                    Debug.Log("Sending DVL configuration...");
                    return GetConfigResponse();
                case "set_config":
                    // check for parameters
                    if (command.parameters != null)
                    {
                        foreach (var param in command.parameters)
                        {
                            Debug.Log($"Config param: {param.Key} = {param.Value}");
                        }
                    }
                    Debug.Log("Updating DVL configuration...");
                    return SendConfigResponse();
                default:
                    Debug.LogWarning($"Unknown command: {command.command}");
                    DVLJSONProtocol.CommandResponse unknownCmd = new DVLJSONProtocol.CommandResponse
                    {
                        response_to = command.command,
                        success = false,
                        error_message = $"Unknown command: {command.command}",
                        result = null,
                        format = PROTOCOL_VERSION,
                        type = "response"
                    };
                    return JsonConvert.SerializeObject(unknownCmd);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("DVLa50SimSender: Failed to process command - " + e.Message);
            DVLJSONProtocol.CommandResponse errorResponse = new DVLJSONProtocol.CommandResponse
            {
                response_to = "unknown",
                success = false,
                error_message = $"Exception: {e.Message}",
                result = null,
                format = PROTOCOL_VERSION,
                type = "response"
            };
            return JsonConvert.SerializeObject(errorResponse);
        }
    }

    string ResetDeadReckoning()
    {
        // Simulate resetting dead reckoning
        // TODO actually reset position to (0,0,0) and maybe add some random noise to simulate realignment uncertainty
        DVLJSONProtocol.CommandResponse reset_dr = DVLJSONProtocol.CommandResponse.GetDefault("reset_dead_reckoning");
        Debug.Log("Dead reckoning reset.");
        return JsonConvert.SerializeObject(reset_dr);
    }

    string CalibrateGyroResponse()
    {
        // Simulate gyro calibration delay
        // TODO actually do calibration
        DVLJSONProtocol.CommandResponse calibrate_gyro = DVLJSONProtocol.CommandResponse.GetDefault("calibrate_gyro");
        Debug.Log("Gyro calibration complete.");
        return JsonConvert.SerializeObject(calibrate_gyro);
    }

    string TriggerPingResponse()
    {
        // Simulate ping delay and response
        // TODO actually trigger each of the 15 external pings
        DVLJSONProtocol.CommandResponse trigger_ping = DVLJSONProtocol.CommandResponse.GetDefault("trigger_ping");
        Debug.Log("DVL ping triggered.");
        return JsonConvert.SerializeObject(trigger_ping);
    }

    string GetConfigResponse()
    {
        // Simulate sending config
        // TODO actually query config parameters
        DVLJSONProtocol.CommandResponse get_config = new DVLJSONProtocol.CommandResponse
        {
            response_to = "get_config",
            success = true,
            error_message = "",
            result = new Dictionary<string, object>
            {
                { "speed_of_sound", 1475.00 },
                { "acoustic_enabled", true},
                { "dark_mode_enabled", false},
                { "mounting_rotation_offset", "20.00"},
                {"range_mode", "auto"},
                {"periodic_cycling_enabled", true}
            },
            format = PROTOCOL_VERSION,
            type = "response"
        };
        Debug.Log("DVL configuration sent.");
        return JsonConvert.SerializeObject(get_config);
    }

    string SendConfigResponse()
    {
        // Simulate setting config
        // TODO actually change parameters where needed
        DVLJSONProtocol.CommandResponse set_config = DVLJSONProtocol.CommandResponse.GetDefault("set_config");
        Debug.Log("DVL configuration updated.");
        return JsonConvert.SerializeObject(set_config);
    }

    void OnApplicationQuit()
    {
        isServerRunning = false; // Signal the server thread to stop
        if (serverThread != null && serverThread.IsAlive)
        {
            serverThread.Join(500); // Wait for the server thread to finish gracefully, abandon if it takes 500ms
            serverThread.Abort(); // Forcefully abort otherwise
        }
    }
}
