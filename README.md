inedox-golang
=============

Contains operations helpful when building programs written in Go.

Operations
----------

- **Compile Go Package** - Builds a Go package using the go build command.
- **Download Go Source Code** - Uses "go get" to download Go source code and dependencies based on an import path.
- **Prepare Go Runtime** - Ensure that a specific version of Go's compiler and standard library is available on the current server.
- **Run Go program from source code** - Compile and run a Go program in one step using the go run command.
- **Test Go Package** - Run test cases on a Go package using the go test command.
- **Update generated Go files** - Runs go generate on a package.

Variable Functions
------------------

- $GoEnv (`Name`, `[GoExecutableName]`) - Returns the value of a Go environment variable.
- **@GoList** (`Pattern`, `[GoExecutableName]`, `[Commands]`) - Returns a list of Go packages that match a pattern.
