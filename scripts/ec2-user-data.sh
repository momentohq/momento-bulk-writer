#!/bin/bash
yum update -y

# set up docker
amazon-linux-extras install docker
systemctl enable docker
systemctl start docker
usermod -a -G docker ec2-user
newgrp docker

# install dotnet runtime
rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
yum install -y aspnetcore-runtime-6.0
yum install -y dotnet-sdk-6.0
