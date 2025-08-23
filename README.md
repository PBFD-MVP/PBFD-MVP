# VisitorLog_PBFD

[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.16883985.svg)](https://doi.org/10.5281/zenodo.16883985)

This repository contains the source code for a Minimum Viable Product (MVP) developed to support the research paper, **"PBFD and PDFD: Formally Defined and Verified Methodologies and Empirical Evaluation for Scalable Full-Stack Software Engineering."**

This is an ASP.NET MVC application that allows users to log their visits to all continents in the world. This application serves as a demonstration of the **Primary Breadth-First Development (PBFD)** methodology.

> ⚠️ **Disclaimer**: The code is provided for research and verification purposes only and is not intended for production use.

## Requirements

To build and run this application, you will need the following development environment. This solution was built and tested with the versions listed below.

- Microsoft Visual Studio Professional 2022
- .NET 6.0 SDK
- SQL Server 2022
- SQL Server Management Studio (SSMS) v.20

## Setup and Installation

Follow these steps to get the application up and running on your local machine.

### 1. Clone the Repository
Clone this repository to your local machine using Git:
```bash
git clone https://github.com/PBFD-MVP/PBFD-MVP.git
```

### 2. Open the Solution
Open the `VisitorLog_PBFD.sln` solution file in Visual Studio 2022. The necessary NuGet packages should be restored automatically. If not, rebuild the solution to trigger the package restore.

### 3. Create the Database
Using SQL Server Management Studio (SSMS), create a new, empty database named `VisitorLog_PBFD`.

### 4. Update Connection String
In your solution, open the `appsettings.json` file. Find the `ConnectionStrings` section and update the `DefaultConnection` value to match your local SQL Server credentials.

### 5. Run Migrations
Open the Package Manager Console in Visual Studio by navigating to:

**Tools > NuGet Package Manager > Package Manager Console**

Run the following command to apply the existing migrations and generate all necessary tables in your new database:

```powershell
Update-Database
```

### 6. Generate Dynamic Tables (First Run Only)
In your solution, open the `appsettings.json` file.

Change the `CreateDatabase` setting to `true`:

```json
"CreateDatabase": true
```

Run the application in Visual Studio (press **F5**). The application will create all dynamic tables and launch in your web browser.

### 7. Subsequent Runs
After the dynamic tables have been generated, close the application.

Change the `CreateDatabase` setting back to `false` to prevent table recreation:

```json
"CreateDatabase": false
```

## Research Implementation Notes

This solution includes several implementation choices specific to its research context:

### Database Initialization & Setup
The database setup process uses a custom initialization mechanism (controlled by the `CreateDatabase` flag in `appsettings.json`) to ensure simplicity and repeatability during **research validation and demonstration**.

*   **In this MVP:** The `CreateDatabase: true` setting automates the generation of dynamic tables on application startup, allowing researchers to quickly get the application running.
*   **In a production environment:** This entire initialization process would be moved to dedicated deployment scripts or a more robust database migration utility to avoid the overhead and potential security risks of generating tables on every execution.

This choice was made to lower the barrier to entry for reproducing the results described in our paper, focusing the research validation on the core PBFD methodology rather than on complex deployment infrastructure.

### Caching Strategy
The application uses in-memory dictionaries for caching performance-sensitive operations:

```csharp
// RESEARCH IMPLEMENTATION: In-memory caches for performance optimization.
// In a production environment, these would be replaced by a distributed cache
// (e.g., Redis) for scalability and consistency across multiple application instances.
private Dictionary<string, List<string>> _tableColumnCache = new();
private Dictionary<string, List<NodeViewModel>> _hierarchyPathChildrenCache = new();
private Dictionary<string, NodeViewModel> _hierarchyPathSingleNodeCache = new();
private Dictionary<string, Dictionary<string, object>> _tableColumnBitmapCache = new();
```

These implementations are sufficient for the single-instance research MVP but would be replaced with distributed caching solutions in a production environment.

## License
This repository is licensed under the **Apache-2.0 License**.

## Contact
For questions regarding this repository or the research it supports, please contact [dliu@us.ibm.com].
