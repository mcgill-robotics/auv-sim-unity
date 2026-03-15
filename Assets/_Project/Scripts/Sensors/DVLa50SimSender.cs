using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class DVLa50SimSender : MonoBehaviour
{
    TcpListener server; // server to listen for incoming connections
    TcpClient client; // connection to client
    NetworkStream stream; // stream for sending data to client
    Thread serverThread;
    bool isServerRunning = false;

    // server parameters
    [Header("Server Configuration")]
    [SerializeField] private int port = 16171; // port to listen on
    [SerializeField] private string ipAddress; // IP address to bind to
    [SerializeField] private int publishRateHz = 10; // how often to send messages (in Hz)


    private void Start()
    {
        // Start the server in a separate thread to avoid blocking the main Unity thread
        serverThread = new Thread(new ThreadStart(SetupServer));
        serverThread.IsBackground = true; // Ensure the thread will close when the application exits
        isServerRunning = true;
        serverThread.Start();
    }

    // Fixed update to match physics update rate to update the stored DVL data report
    void FixedUpdate()
    {

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
                // Accept incoming client connections asynchronously
                using (TcpClient client = await server.AcceptTcpClientAsync())
                // start network stream for the connected client
                using (NetworkStream stream = client.GetStream())
                {
                    Debug.Log($"Client connected: {client.Client.RemoteEndPoint}");
                    // return streamdata async task
                    await StreamDataAsync(stream);
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
            stream?.Close();
            client?.Close();
            server?.Stop();
        }
    }

    async private System.Threading.Tasks.Task StreamDataAsync(NetworkStream stream)
    {
        try
        {
            while (isServerRunning)
            {
                // Create a test message (replace with actual DVL data in practice)
                string message = "Hello from DVLa50SimSender!" + '\n';
                byte[] data = Encoding.ASCII.GetBytes(message);

                // Send the message to the client asynchronously
                await stream.WriteAsync(data, 0, data.Length);

                // Wait for the next publish interval
                await System.Threading.Tasks.Task.Delay(1000 / publishRateHz);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("DVLa50SimSender: Exception in StreamDataAsync - " + e.Message);
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
