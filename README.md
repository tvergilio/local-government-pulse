# Local Government Pulse

Local Government Pulse is a real-time Kafka stream processing system built using .NET 8. This project demonstrates how to process streaming data for local government issues using the Streamiz Kafka.NET library, which enables Kafka Streams on the .NET ecosystem. Kafka Streams is a client library for building applications and microservices, where the input and output data are stored in Kafka clusters. It simplifies the development of highly scalable and fault-tolerant applications that process real-time streams of data.

### Key Technologies:
- **.NET 8**: The latest version of the .NET platform for building high-performance applications.
- **Kafka**: A distributed event streaming platform for handling high-throughput data streams.
- **Streamiz.Kafka.Net**: A library enabling Kafka Streams on .NET, allowing stream processing applications to be built and run within the .NET ecosystem.
- **Docker**: Containerisation of the services required for seamless deployment.

### System Components:
- **Kafka Topics**: Used to ingest and emit data (e.g., `input-topic` for incoming messages, `output-topic` for results).
- **Stream Processing**: Data from Kafka is processed using the Streamiz library, transforming values before output.

### Data Processing Flow:
1. **Ingest**: Stream messages are received from `input-topic`.
2. **Process**: Messages are transformed by mapping values to uppercase.
3. **Emit**: The transformed messages are sent to `output-topic`.

---

### Setting Up and Running the Project

#### Prerequisites:
- Docker and Docker Compose installed on your machine.

#### Clone the Repository:
```bash
git clone https://github.com/tvergilio/local-government-pulse
cd local-government-pulse
```

#### Build and Run the Docker Containers:
```bash
docker-compose up --build
```

#### Creating Kafka Topics:
```bash
docker-compose exec kafka kafka-topics --create --topic slack_messages --partitions 1 --replication-factor 1 --bootstrap-server kafka:9092
docker-compose exec kafka kafka-topics --create --topic results --partitions 1 --replication-factor 1 --bootstrap-server kafka:9092
```

---

### Running Tests
The project uses unit tests for stream processing logic. To run the tests:
```bash
dotnet test
```

---

### Authors:
Thalita Vergilio

### License:
This project is licensed under the MIT License.