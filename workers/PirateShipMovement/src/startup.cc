#include <improbable/worker.h>
#include <improbable/standard_library.h>
#include <iostream>
#include <algorithm>
#include <ctime>
#include <improbable/view.h>
#include <improbable/ship/ShipControls.h>
#include <chrono>
#include <thread>

// Enables the nice 5s syntax for time
using namespace std::chrono_literals;

// Use this to make a worker::ComponentRegistry.
// For example use worker::Components<improbable::Position, improbable::Metadata> to track these common components
using ShipComponents = worker::Components<improbable::ship::ShipControls, improbable::Position>;

// Constants and parameters
const int ErrorExitStatus = 1;
const std::string kLoggerName = "startup.cc";
const std::uint32_t kGetOpListTimeoutInMilliseconds = 100;

worker::Connection ConnectWithReceptionist(const std::string hostname,
    const std::uint16_t port,
    const std::string& worker_id,
    const worker::ConnectionParameters& connection_parameters)
{
    auto future = worker::Connection::ConnectAsync(ShipComponents{}, hostname, port, worker_id, connection_parameters);
    return future.Get();
}

std::string get_random_characters(size_t count)
{
    const auto randchar = []() -> char
    {
        const char charset[] =
            "0123456789"
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
            "abcdefghijklmnopqrstuvwxyz";
        const auto max_index = sizeof(charset) - 1;
        return charset[rand() % max_index];
    };
    std::string str(count, 0);
    std::generate_n(str.begin(), count, randchar);
    return str;
}

// Entry point
int main(int argc, char** argv)
{
    srand(time(nullptr));

    std::cout << "[local] Worker started " << std::endl;

    auto print_usage = [&]()
    {
        std::cout << "Usage: PirateShipMovement receptionist <hostname> <port> <worker_id>" << std::endl;
        std::cout << std::endl;
        std::cout << "Connects to SpatialOS" << std::endl;
        std::cout << "    <hostname>      - hostname of the receptionist or locator to connect to.";
        std::cout << std::endl;
        std::cout << "    <port>          - port to use if connecting through the receptionist.";
        std::cout << std::endl;
        std::cout << "    <worker_id>     - (optional) name of the worker assigned by SpatialOS." << std::endl;
        std::cout << std::endl;
    };

    std::vector<std::string> arguments;

    // if no arguments are supplied, use the defaults for a local deployment
    if (argc == 1)
    {
        arguments = {"receptionist", "localhost", "7777"};
    }
    else
    {
        arguments = std::vector<std::string>(argv + 1, argv + argc);
    }

    if (arguments.size() != 4 && arguments.size() != 3)
    {
        print_usage();
        return ErrorExitStatus;
    }

    worker::ConnectionParameters parameters;
    parameters.WorkerType = "PirateShipMovement";
    parameters.Network.ConnectionType = worker::NetworkConnectionType::kTcp;
    parameters.Network.UseExternalIp = false;

    std::string workerId;

    // When running as an external worker using 'spatial local worker launch'
    // The WorkerId isn't passed, so we generate a random one
    if (arguments.size() == 4)
    {
        workerId = arguments[3];
    }
    else
    {
        workerId = parameters.WorkerType + "_" + get_random_characters(4);
    }

    std::cout << "[local] Connecting to SpatialOS as " << workerId << "..." << std::endl;

    // Connect with receptionist
    worker::Connection connection = ConnectWithReceptionist(arguments[1], atoi(arguments[2].c_str()), workerId, parameters);

    connection.SendLogMessage(worker::LogLevel::kInfo, kLoggerName, "Connected successfully");

    // Register callbacks and run the worker main loop.
    bool is_connected = connection.IsConnected();

    // Create a view
    worker::View view{ShipComponents{}};

    // Generates random floats between two values
    auto rand_float = [](float low, float high)
    {
        return low + static_cast <float> (rand()) / (static_cast <float> (RAND_MAX / (high - low)));
    };

    view.OnDisconnect([&](const worker::DisconnectOp& op)
        {
            std::cerr << "[disconnect] " << op.Reason << std::endl;
            is_connected = false;
        });

    // Print log messages received from SpatialOS
    view.OnLogMessage([&](const worker::LogMessageOp& op)
        {
            if (op.Level == worker::LogLevel::kFatal)
            {
                std::cerr << "Fatal error: " << op.Message << std::endl;
                std::terminate();
            }
            std::cout << "[remote] " << op.Message << std::endl;
        });

    if (is_connected)
    {
        std::cout << "[local] Connected successfully to SpatialOS, listening to ops... " << std::endl;
    }

    // Holds each component update before sending it
    improbable::ship::ShipControls::Update steering_update;

    while (is_connected)
    {
        auto ops = connection.GetOpList(kGetOpListTimeoutInMilliseconds);

        // Process the list of Ops by the view
        view.Process(ops);

        for (auto iter = view.Entities.begin(); iter != view.Entities.end(); ++iter)
        {
            auto entity_id = iter->first;
            // Randomize speed and steering for each ship
            steering_update.set_target_speed(rand_float(0.0, 1.0));

            // The change in steering is small to make sure ships don't suddenly turn around
            steering_update.set_target_steering((rand_float(-15.0, 15.0)));

            // Send the steering update to SpatialOS
            connection.SendComponentUpdate<improbable::ship::ShipControls>(entity_id, steering_update);
        }

        // Add a delay between each iteration to avoid excess steering changes
        std::this_thread::sleep_for(5s);
    }

    return ErrorExitStatus;
}
