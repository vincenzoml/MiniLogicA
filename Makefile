publish:
	# /p:PublishReadyToRun=true
	dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true