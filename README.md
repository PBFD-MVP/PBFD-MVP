**Project Overview**

VisitorLog_PBFD is an ASP.NET MVC application that allows users to log their visits to all continents in the world. This application serves as a minimal viable product (MVP) to demonstrate the Primary Breadth-First Development (PBFD) methodology.

**Prerequisites**

To build and run this application, you will need:
* Microsoft Visual Studio Professional 2022
* SQL Server 2022
* SQL Server Management Studio (SSMS) v.20

Note: While other versions may work, this solution was built and tested exclusively with the versions listed above.

**Setup Instructions**

Follow these steps to set up the database and run the application for the first time.

1. Download the Solution: Clone or download the solution file to your local machine.

2. Open in Visual Studio: Open the solution file (.sln) in Visual Studio. The necessary NuGet packages should be restored automatically. If not, rebuild the solution to download them.

3. Update the Connection String:

    * Open the appsettings.json file in your solution.

    * Find the ConnectionStrings section and update the DefaultConnection value to match the server name, database name, and user credentials of your local SQL Server environment.

4. Create the Database: Using SQL Server Management Studio (SSMS), create a new empty database named VisitorLog_PBFD.

5. Generate Static Tables:

    * In Visual Studio, go to Tools > NuGet Package Manager > Package Manager Console.

    * In the console, type the following command and press Enter:

      *Update-Database*

    * This command will apply the existing migrations and generate all static tables in your new VisitorLog_PBFD database.

6. Generate Dynamic Tables (First Run Only):

    * In your solution, open the appsettings.json file.

    * Change the "CreateDatabase" setting to true:

      *"CreateDatabase": true*

    * Run the application in Visual Studio (e.g., by pressing F5). The application will create all the dynamic tables and then launch in your web browser.

7. Subsequent Runs:

    * After the dynamic tables have been generated, close the application.

    * Go back to the appsettings.json file and change the "CreateDatabase" setting back to false to prevent the tables from being recreated on subsequent executions.
  
**Notes on Database Setup**

This solution includes a custom mechanism to generate dynamic tables for simplicity during development. In a production environment, you should move steps 6-8 to a standalone utility or a more robust database management solution to avoid generating tables every time the application is executed.
