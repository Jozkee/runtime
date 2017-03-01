#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)

toolsLocalPath=$__scriptpath/Tools
bootStrapperPath=$toolsLocalPath/bootstrap.sh

if [ ! -e $bootStrapperPath ]; then
    if [ ! -e $toolsLocalPath ]; then
        mkdir $toolsLocalPath
    fi
    cp $__scriptpath/bootstrap.sh $__scriptpath/Tools
fi

$bootStrapperPath --repositoryRoot $__scriptpath --toolsLocalPath $toolsLocalPath > bootstrap.log
lastExitCode=$?
if [ $lastExitCode -ne 0 ]; then
    echo "Boot-strapping failed with exit code $lastExitCode, see bootstrap.log for more information."
    exit $lastExitCode
fi

dotNetExe=$toolsLocalPath/dotnetcli/dotnet
runExe=$toolsLocalPath/microsoft.dotnet.buildtools.run/netcoreapp1.0/run.exe
echo $dotNetExe $runExe $@
$dotNetExe $runExe $@
exit $?
