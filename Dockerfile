# PERSONALIZANDO IMAGEN
# Imagen base en la cual basaremos nuestra imagen
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# Creación del directorio de trabajo
WORKDIR /app
# Exponemos el puerto 
# EXPOSE 80
# Copiar del csproj y restauramos nuestra app
COPY ./*.csproj ./
RUN dotnet restore
# Copiamos todos los archivos y compilados o construidos de nuestra app
COPY . .
RUN dotnet publish -c Release -o publish
# Construir o instanciamos nuestro contenedor
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/publish .
# Indicar el archivo dll compilado (Nombre del Proyecto)
ENTRYPOINT [ "dotnet", "WorkerQueueManagement.dll" ]