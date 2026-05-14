# TeruTeruServer Technical Documentation for AI

This document provides a comprehensive technical overview of the TeruTeruServer project. It is designed for an AI with a large context window (up to 1M tokens) to understand the architecture, logic flow, and implementation details of the system.

---

## 1. Project Overview

**TeruTeruServer** is a high-performance, scalable C# server engine designed for real-time communication, P2P signaling, and group-based interactions. It utilizes an IOCP-inspired asynchronous socket model (`SocketAsyncEventArgs`), a middleware-based packet processing pipeline, and a modular plugin-based architecture for business logic.

### Key Goals:
- **Scalability**: Handle high concurrent connections using asynchronous I/O.
- **Modularity**: Separation of concerns via a 4-tier architecture.
- **Maintainability**: Dependency Injection (DI) and clean interfaces.
- **Flexibility**: Hot-loading logic plugins and a customizable middleware pipeline.
- **P2P Support**: Built-in signaling for UDP Hole Punching and server-side relay fallback.

---

## 2. Architecture: The 4-Tier Model

The system is strictly divided into four layers to ensure unidirectional dependency and clean separation.

### 2.1 `TeruTeruServer.SDK` (Shared Core)
- **Role**: Definition of contracts, models, and shared utilities.
- **No Dependencies**: Does not depend on any other project in the solution.
- **Key Components**:
    - **Interfaces**: `ILogicService`, `ISessionManager`, `IMessageSender`, `IProtocolRouter`.
    - **Enums**: `SendType` (Direct/Json), `ProtocolSelect`, `SessionState` (Connected/Grace/Disconnected).
    - **Models**: `RpcRequest`, `P2PGroup`, `CommonProtocols`.
    - **Utilities**: Encryption (AES, Seed), `PacketUtility`, `TeruTeruLogger`.
    - **Session**: `ClientSession` object tracking connection state, tokens, and P2P status.

### 2.2 `TeruTeruServer.Runtime` (Server Engine)
- **Role**: The "Heart" of the server. Manages networking, sessions, and the processing pipeline.
- **Key Components**:
    - **`MainServer`**: Orchestrates TCP/UDP listeners, accepts connections, and initiates the receive loop.
    - **`PacketPipeline`**: A middleware chain that processes incoming `PacketContext`.
    - **`SessionManager`**: Manages the lifecycle of `ClientSession` objects, including "Grace" period handling for reconnections.
    - **`RpcProxy` & `ProtocolRouter`**: Dispatches packets to the appropriate handlers based on attributes.

### 2.3 `TeruTeruServer.Logic.Default` (Business Logic Plugin)
- **Role**: Implementation of specific service logic.
- **Implementation**: Implements `ILogicService`.
- **Key Handlers**:
    - **`P2PSignalingHandler`**: Manages Hole Punching requests and endpoint exchange.
    - **`P2PRelayHandler`**: Handles fallback relaying when direct P2P fails.
    - **`P2PGroupHandler`**: Manages logical groups for multi-user interactions.
    - **`LogicPlugin`**: The main entry point for the plugin, containing methods decorated with `[Protocol]` or `[Rpc]` attributes.

### 2.4 `TeruTeruServer.Cli` (Entry Point)
- **Role**: Host application. Sets up DI, loads configuration, and starts the `MainServer`.
- **Key Components**:
    - **`ConfigManager`**: Reads `config.txt`.
    - **`PluginManager`**: Dynamically loads `.dll` files from the `plugins/` directory and can monitor for changes (Hot-loading).

---

## 3. Network & Protocol Specification

### 3.1 Packet Structure
Every packet sent or received follows a specific byte-level format.

1. **Header (2 Bytes)**:
    - `[0]`: `SendType` (0: Direct, 1: Json)
    - `[1]`: `ProtocolSelect` (Identifier for the protocol)
2. **Payload (Variable)**:
    - If `SendType.Json`: A UTF-8 encoded JSON string.
    - If `SendType.Direct`: Raw binary data (often prefixed with extra headers like JWT for security).

### 3.2 Key Protocols (`ProtocolSelect`)
- `ConnectProtocol (1)`: Initial connection handshake.
- `LoginProtocol (2)`: Authentication and JWT issuance.
- `ReconnectProtocol (3)`: Resuming a session using a `ReconnectToken`.
- `UdpRegisterProtocol (4)`: Registering the public UDP endpoint for Hole Punching.
- `HolePunchRequest (5)`: Requesting peer information for P2P.
- `RpcProtocol (100)`: General-purpose Remote Procedure Call.

---

## 4. Packet Processing Pipeline

The `MainServer` passes every received byte array into the `PacketPipeline`. The pipeline executes a series of middlewares in order:

1. **`ValidationMiddleware`**: Checks if the packet meets the minimum length and basic header requirements.
2. **`DecryptionMiddleware`**: Decrypts the payload if it is encrypted.
3. **`AuthMiddleware`**:
    - For sensitive protocols, it extracts the JWT from the payload.
    - Validates the token against the `SessionManager`.
    - Attaches the authenticated `ClientSession` to the `PacketContext`.
4. **`RoutingMiddleware`**: Uses the `IProtocolRouter` to find and invoke the method in the `LogicPlugin` that matches the `ProtocolSelect`.

---

## 5. P2P & Group Logic

The server facilitates P2P communication through a tiered approach:

1. **UDP Registration**: Clients send a `Direct` packet via UDP to the server. The server records their public IP/Port in `ClientSession.UdpEndPoint`.
2. **Signaling**: When Client A wants to connect to Client B, it sends a `HolePunchRequest`. The server sends Client B's public endpoint to A, and Client A's public endpoint to B.
3. **Relay Fallback**: If Hole Punching fails (due to Symmetric NAT, etc.), clients send packets using `P2PRelayProtocol` or `GroupRelayProtocol`. The server then forwards these packets to the intended recipients.

---

## 6. Session Lifecycle & Reconnection

- **Connected**: Active socket connection.
- **Grace**: When a socket disconnects, the session is moved to `Grace` state for 30 seconds (configurable).
- **Reconnection**: A client can reconnect within the grace period using a `ReconnectToken`. If successful, the new socket is linked to the existing `ClientSession`, preserving state.
- **Eviction**: If the grace period expires, the session is fully removed, and cleanup logic (e.g., removing from groups) is triggered.

---

## 7. Development & Extensibility

### Adding New Logic
To add a new feature:
1. Define a new protocol in `ProtocolSelect` (SDK).
2. Add a method in `LogicPlugin` (Logic) and decorate it with `[Protocol(ProtocolSelect.YourProtocol)]`.
3. If using RPC, decorate with `[Rpc("MethodName")]`.
4. Rebuild the logic project and place the DLL in the `plugins/` folder.

### Configuration (`config.txt`)
Parameters include:
- `Port`: Server listening port.
- `MaxConnection`: Maximum allowed sessions.
- `IsTcp` / `IsUdp`: Protocol toggle.
- `Guid`: Unique server identifier.
- `SendBufferSize` / `ReceiveBufferSize`: Network tuning.

---

## 8. Database Integration

The server provides a standard interface for database operations via `IDatabaseService`.
- **`DatabaseConnector`**: A wrapper for MySQL connections using `MySql.Data`.
- **`DatabaseHelper`**: Implements `IDatabaseService` and provides methods for executing scalars, readers, and non-query commands.
- **DI Usage**: The database service is typically registered in the DI container during startup, allowing logic plugins to perform persistence operations seamlessly.

---

## 9. Console Commands

The server includes a command-line interface for administrative tasks.
- **`CommandHandler`**: Parses console input and dispatches it to `ICommand` implementations.
- **Key Commands**:
    - `exit`: Gracefully shuts down the server.
    - `Queue_Count`: Displays the current number of pending tasks/messages.
    - `Worker_Start`: Manages background worker threads.
    - `2` (ImageDump): Triggers an image dump for debugging.

---

## 10. Critical Implementation Details (Internal Reference)

- **`MainServer.cs`**: Uses `SocketAsyncEventArgs` to avoid per-connection thread overhead. The `AcceptLoop` and `ReceiveLoop` are fully asynchronous.
- **`ServerMemory.cs`**: A static repository in the SDK used for quick lookup of sessions by HostID or GameID.
- **`PacketContext.cs`**: Carries the `Socket`, `Data`, and `ClientSession` through the middleware pipeline.
- **Thread Safety**: Uses `ConcurrentDictionary` for session and group management.

---

## Summary for AI Context
When working on this codebase, prioritize keeping the SDK pure and implementation-agnostic. Business logic should always reside in the Logic project/plugins, and network-level optimizations or pipeline changes should be made in the Runtime project. Always ensure that new protocols are properly integrated into the middleware pipeline (especially `AuthMiddleware` if they require security).
