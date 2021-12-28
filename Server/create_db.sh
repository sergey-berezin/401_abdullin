#!/bin/bash

dotnet tool install --global dotnet-ef --version 5.0
dotnet add package Microsoft.EntityFrameworkCore.Design

dotnet ef migrations add InitialCreate
dotnet ef database update
