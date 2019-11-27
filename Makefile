test:
	dotnet run testGraph.json

publish:
	dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true	
	dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
	dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true

clean:
	rm -rf bin obj
