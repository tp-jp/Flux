# Flux

Flux is a lightweight GPGPU framework for Unity.

It provides a low-level foundation for GPU-based data processing and simulations, designed to support reusable higher-level systems such as particle effects and other GPU-driven applications.

> [!WARNING]
> Flux is currently in early development.
> APIs and package structure may change without notice.

## Features

- GPU-based data processing
- ComputeBuffer management
- Dispatch utilities
- Ping-pong buffer processing
- Reduction operations

## Requirements

- Unity 2022.3 or later

## Installation

Flux can be installed through the Unity Package Manager.

Open **Package Manager > Add package from git URL...** and enter the Git repository URL:

```text id="rcv75a"
https://github.com/<OWNER>/<REPOSITORY>.git
```

You can also install a specific version using a Git tag:

```text id="c41yn7"
https://github.com/<OWNER>/<REPOSITORY>.git#0.0.1
```

## Packages

Flux is designed to serve as a foundation for higher-level packages.

### Flux

The core package providing low-level GPGPU functionality and utilities.

### FluxFX

A GPU particle and effects system built on Flux.

FluxFX is developed and distributed separately from Flux.

## Status

Flux is currently in early development and intended for testing and evaluation.

Breaking changes may occur in future releases.

## License

See the `LICENSE` file for details.