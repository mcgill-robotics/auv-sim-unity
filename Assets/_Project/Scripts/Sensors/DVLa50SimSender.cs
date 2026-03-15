using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
public enum DVLStatus : byte
{
    //bit mask where bit 0 set 1 to is high temperature, 0 otherwise
    OK = 0,
    HighTemperature = 1 << 0 // shift 1 to the left by 0 positions to set bit 0 to 1
}

// from https://docs.waterlinked.com/dvl/dvl-protocol/
[Serializable]
class DVLTransducer
{
    public int id;
    public double velocity;
    public double distance;
    public double rssi;
    public double nsi;
    public bool beam_valid;
}
[Serializable]
class DVLVelocityReport
{
    public long time;
    public double vx;
    public double vy;
    public double vz;
    public double fom;
    public double[][] covariance;
    public double altitude;
    public List<DVLTransducer> transducers;
    public bool velocity_valid;
    public byte status;
    public long time_of_validity;
    public long time_of_transmission;
    public string format;
    public string type;
}

[Serializable]
class DVLDeadReckoningReport
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


public class DVLa50SimSender : MonoBehaviour
{
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
    DVLDeadReckoningReport currentDeadReckoningReport; // store the most recent DVL dead reckoning report to send to clients when they request it
    DVLVelocityReport currentVelocityReport; // same for velocity
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
            currentVelocityReport = new DVLVelocityReport
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
                transducers = new List<DVLTransducer>
                    {
                        new DVLTransducer
                        {
                            id = 0,
                            velocity = UnityEngine.Random.Range(-5f, 5f),
                            distance = UnityEngine.Random.Range(0.1f, 10f),
                            rssi = UnityEngine.Random.Range(0.1f, 1f),
                            nsi = UnityEngine.Random.Range(0.1f, 1f),
                            beam_valid = true
                        },
                        new DVLTransducer
                        {
                            id = 1,
                            velocity = UnityEngine.Random.Range(-5f, 5f),
                            distance = UnityEngine.Random.Range(0.1f, 10f),
                            rssi = UnityEngine.Random.Range(0.1f, 1f),
                            nsi = UnityEngine.Random.Range(0.1f, 1f),
                            beam_valid = true
                        },
                        new DVLTransducer
                        {
                            id = 2,
                            velocity = UnityEngine.Random.Range(-5f, 5f),
                            distance = UnityEngine.Random.Range(0.1f, 10f),
                            rssi = UnityEngine.Random.Range(0.1f, 1f),
                            nsi = UnityEngine.Random.Range(0.1f, 1f),
                            beam_valid = true
                        },
                        new DVLTransducer
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
                status = (byte)DVLStatus.OK,
                time_of_validity = (long)UnityEngine.Random.Range(lastPublishTime, currentTime), // choose random time between last publish and now for when the ping reached the bottom
                time_of_transmission = currentTime,
                format = "json_v3.1",
                type = "velocity"
            };
            currentDeadReckoningReport = new DVLDeadReckoningReport
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
                status = (byte)DVLStatus.OK,
                format = "json_v3"
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
                string jsonVelocityReport;
                string jsonPositionReport;
                // Check if the DVL reports have been initialized yet, if not skip this publish cycle to avoid sending empty data to clients
                if (currentVelocityReport == null || currentDeadReckoningReport == null)
                {
                    Debug.LogWarning("DVL reports not initialized yet, skipping this publish cycle.");
                    await System.Threading.Tasks.Task.Delay(100); // wait a bit before trying again
                    continue;
                }
                // lock and quickly serialize velocity and position reports
                lock (_reportLock)
                {
                    jsonPositionReport = JsonConvert.SerializeObject(currentDeadReckoningReport);
                    jsonVelocityReport = JsonConvert.SerializeObject(currentVelocityReport);
                }

                SendLine(stream, jsonPositionReport);
                SendLine(stream, jsonVelocityReport);

                // Wait for the next publish interval
                await System.Threading.Tasks.Task.Delay(1000 / publishRateHz);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("DVLa50SimSender: Exception in StreamDataAsync - " + e.Message);
        }
    }

    async private void SendLine(NetworkStream stream, string jsonString)
    {
        try
        {
            // separate messages with newline for easy parsing on client side\
            byte[] data = Encoding.ASCII.GetBytes(jsonString + '\n');
            lock (_streamWriteLock)
            {
                // Send the message to the client asynchronously
                stream.Write(data, 0, data.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DVL Server] Failed to write to stream: {e.Message}");
        }
    }
    async private System.Threading.Tasks.Task ReadDataFromClient(NetworkStream stream)
    {
        while (isServerRunning && client.Connected && stream.CanWrite)
        {
            if (stream.DataAvailable)
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true))
                {
                    string jsonCommand = await reader.ReadLineAsync();
                    if (jsonCommand == null) break; // Client disconnected
                    Debug.Log($"Received command from client: {jsonCommand}");
                }
            }
        }
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
