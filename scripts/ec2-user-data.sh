#!/bin/bash
yum update -y

# install dotnet runtime
rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
yum install -y aspnetcore-runtime-6.0
yum install -y dotnet-sdk-6.0

# install java (necessary for rdbcli)
yum install -y java
