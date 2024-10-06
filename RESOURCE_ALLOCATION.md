# Resource Allocation

This section explains the resource allocation used for each component in the project. The setup is based on running all components on a **MacBook Pro** (M3, Sonoma, 36 GB memory). The allocation has been carefully estimated to ensure smooth performance with approximately 80 users interacting with the system during a live demonstration.

## Resource Allocation Table

| Component          | Limits                  | Reservations            | Rationale |
| ------------------ | ----------------------- | ----------------------- | --------- |
| **Zookeeper**      | `0.5 CPUs, 512MB RAM`   | `0.5 CPUs, 512MB RAM`   | Zookeeper does not require high processing power, but stable memory allocation ensures consistent Kafka coordination. |
| **Kafka**          | `2 CPUs, 2GB RAM`       | `1.5 CPUs, 1.5GB RAM`   | Kafka brokers need enough memory and CPU to handle message throughput and keep up with the producers and consumers without lag. |
| **Stream Processor** | `1.5 CPUs, 3GB RAM`     | `1.0 CPUs, 2GB RAM`     | The stream processor interacts with the external Gemini API, but enough memory and CPU are required to manage data flow and buffering effectively, especially during high-traffic moments. |
| **Redis**          | `1.0 CPUs, 2GB RAM`     | `0.5 CPUs, 1GB RAM`     | Redis is used as a cache and needs adequate memory for storing trending data and handling evictions without causing bottlenecks. |
| **Slack Producer** | `0.5 CPUs, 512MB RAM`   | `0.5 CPUs, 512MB RAM`   | The Slack Producer runs a Java-based application that handles incoming Slack messages. The load is light, with minimal CPU and memory requirements. |
| **Redis Consumer** | `1.0 CPUs, 1GB RAM`     | `0.5 CPUs, 512MB RAM`   | Consumes data from Kafka and updates Redis. Requires stable memory allocation to process streaming data and keep Redis updated. |
| **WebSocket Consumer** | `1.0 CPUs, 1.5GB RAM`  | `0.5 CPUs, 1GB RAM`     | Handles communication with the front-end via SignalR. Requires more memory for managing concurrent client connections and pushing updates in real time. |

## Rationale for Resource Allocation

### Stream Processor
The **Stream Processor** has a higher memory allocation, even though the actual data processing is done via an external API (Gemini). This allocation is to manage the overhead of processing Kafka streams, handling failures, and ensuring messages are processed without interruptions. While the API calls are external, the stream processor must maintain consistency in the data flow and manage intermediate states effectively.

### Redis
Redis is used for state management, sentiment averaging, and storing trending topics. The memory allocation includes sufficient space to prevent evictions of important data during demos. The **eviction policy** is also set to `volatile-lru` to evict the least recently used keys when memory runs low, without interrupting key data operations.

### CPU and Memory Limits
The CPU and memory limits defined ensure that each container can effectively handle its workload without overwhelming the host system. The **reservations** ensure that the necessary minimum resources are always available, preventing any critical component from running out of resources under high load.

## Considerations
- The original project uses a **MacBook Pro M3** with **36GB of RAM**, which is a high-end setup capable of handling multiple containers simultaneously.
- **Redis** acts as a manual buffer, and its eviction policy is crucial for managing memory usage.
