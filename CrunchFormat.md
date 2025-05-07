# Crunch Package Format

## Overview
Note that if an entry is missing, assume that it follows the format of the previous specification, unless otherwise stated.

### `2.2.0+`
#### File Table
| Name | Description | Type |
|------|-------------|------|
| Hash | Hash of the name of the file | `uint64` |
| Name | Name of the file | `null-term string` |
| Compressed | Whether the file is compressed or not. Non-zero indications compression | `uint8` |
| Size | Size of the file in bytes | `uint64` |
| Offset | Offset to the start of the file data (relative to the data section. First file will always be at `0x00000000`) | `uint64` |

#### Data Section
While the data section is format follows the previous specification, the data now may be compressed.
Refer to the file table to check whether data is compressed or not

### `2.0.0` - `2.1.0`
#### Header
| Name | Description | Type |
|------|-------------|------|
| Magic | Magic number used for detecting valid packages. Will always store `0x434E5243` which when stored in little endian, will spell `CRNC` in a hex editor | `uint32` |
| Version | Version of the package format. Stores all version numbers in a single `uint32`. The most significant byte is always 0. The second stores the major, then the minor, and then path in least significant. For example, once stored in little endian `0x00010200` is version `2.1.0` | `uint32` |
| File Count | Number of files in the package | `uint64` |
| File Offset | Offset to the start of the file table. Should be the same size as the header | `uint64` |
| Data Offset | Offset to the start of the data section | `uint64` |

#### File Table
| Name | Description | Type |
|------|-------------|------|
| Hash | Hash of the name of the file | `uint64` |
| Name | Name of the file | `null-term string` |
| Size | Size of the file in bytes | `uint64` |
| Offset | Offset to the start of the file data (relative to the data section. First file will always be at `0x00000000`) | `uint64` |

#### Data Section
The data section is a contiguous block of data that contains the actual files. Files are not guarenteed to be stored in order. The files are not null-terminated unless the file contents themselves contained one.