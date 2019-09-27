# Starcounter.Techempower

[Techempower](https://www.techempower.com/benchmarks/) benchmarks using Starcounter.

## Setup

`Starcounter.Techempower` requires Starcounter 3.0, which is in preview stage as of 2019.09.26.

### Setup Steps

- Clone the `Starcounter.Techempower` repository.
- Create `artifacts` folder on the same level as `Starcounter.Techempower` folder.
- Download the latest available Starcounter 3.0 from [starcounter.io](https://starcounter.io/download/) and unzip it into the `artifacts` folder.
- Build `Starcounter.Techempower` with `dotnet` CLI or with Visual Studio 2019.

The end folder structure should look like this:

```
|- artifacts
|--- Starcounter.Nova.App.3.0.0-*.nupkg
|--- Other Starcounter NuGet packages.
|- Starcounter.Techempower
|--- src
|------ Starcounter.Techempower.csproj
|------ Other source files.
```

*Note: depending on your Operating System you might need to install some extra Starcounter dependencies specified on the download page.*

## Benchmark handlers

### `db` - Single Query

In this test, each request is processed by fetching a single row from a simple database table. That row is then serialized as a JSON response.

`wrk -t 4 -c 512 -d (time) http://hostname:5000/db`

The handler selects a random `World` using `Db.SQL<World>("SELECT... WHERE Id = ?", id).First()` and returns it as a JSON string.

### `queries` - Multiple Queries

In this test, each request is processed by fetching multiple rows from a simple database table and serializing these rows as a JSON response. The test is run multiple times: testing 1, 5, 10, 15, and 20 queries per request. All tests are run at 512 concurrency.

 `wrk -t 4 -c 512 -d (time) http://hostname:5000/queries?queries=20`.

### `fortunes` - Fortunes

In this test, the framework's ORM is used to fetch all rows from a database table containing an unknown number of Unix fortune cookie messages (the table has 12 rows, but the code cannot have foreknowledge of the table's size).
An additional fortune cookie message is inserted into the list at runtime and then the list is sorted by the message text. Finally, the list is delivered to the client using a server-side HTML template.
The message text must be considered untrusted and properly escaped and the UTF-8 fortune messages must be rendered properly.

`wrk -t 4 -c 512 -d (time) http://hostname:5000/fortunes`

### `updates` - Data updates

This test exercises database writes.
Each request is processed by fetching multiple rows from a simple database table, converting the rows to in-memory objects, modifying one attribute of each object in memory, updating each associated row in the database individually,
and then serializing the list of objects as a JSON response. The test is run multiple times: testing 1, 5, 10, 15, and 20 updates per request.
Note that the number of statements per request is twice the number of updates since each update is paired with one query to fetch the object. All tests are run at 512 concurrency.

 `wrk -t 4 -c 512 -d (time) http://hostname:5000/updates?queries=20`.

## Setup official `tfb` Techempower docker runner

*Note: this instructions are applicable to [`TechEmpower/FrameworkBenchmarks/commit/a05701bb166ff16ec4d4e154ef4cd67442cb34ba`](https://github.com/TechEmpower/FrameworkBenchmarks/commit/a05701bb166ff16ec4d4e154ef4cd67442cb34ba) and might not work with further updated in the `TechEmpower/FrameworkBenchmarks` repository.*

Some resources:

- [Source Code: `TechEmpower/FrameworkBenchmarks`](https://github.com/TechEmpower/FrameworkBenchmarks).
- [Docker Hub: `techempower/tfb`](https://hub.docker.com/r/techempower/tfb).
- [WRK Docker Image: `williamyeh/wrk`](https://hub.docker.com/r/williamyeh/wrk).
- [Running TechEmpower Web Framework Benchmarks on AWS on my own](https://richardimaoka.github.io/blog/techempower-on-aws/).

### 1. Update `benchmark_config.json` file

Add the following entry to the `/FrameworkBenchmarks/frameworks/CSharp/aspnetcore/benchmark_config.json` file:

```json
"starcounter": {
    "fortune_url": "/fortunes",
    "db_url": "/db",
    "update_url": "/updates?queries=",
    "query_url": "/queries?queries=",
    "port": 8080,
    "approach": "Realistic",
    "classification": "Platform",
    "database": "None",
    "framework": "ASP.NET Core",
    "language": "C#",
    "orm": "Raw",
    "platform": ".NET",
    "flavor": "CoreCLR",
    "webserver": "Kestrel",
    "os": "Linux",
    "database_os": "Linux",
    "display_name": "ASP.NET Core, Starcounter",
    "notes": "",
    "versus": "aspcore"
}
```

### 2. Update `test_types.py`

Add the following `elif` statement into the `get_current_world_table` function of the `/FrameworkBenchmarks/toolset/benchmark/test_types/framework_test_type.py` file to properly verify `update` benchmark with `none` database.

```
elif database_name == "none":
	try:
		worlds_json = urllib2.urlopen("http://tfb-server:8080/select/worlds").read()
		worlds_json = json.loads(worlds_json);
		results_json.append(worlds_json)
	except Exception:
		tb = traceback.format_exc()
		log("ERROR: Unable to load current Starcounter World table.",
			color=Fore.RED)
		log(tb)
```

Add `import urllib2` at the top of the file.

*Note: this will break all other update benchmarks with `none` database.*

### 3. Copy framework implementation files

Create `/FrameworkBenchmarks/frameworks/CSharp/aspnetcore/Starcounter.Techempower` folder and copy into it all application files from the [`src`] folder,
also copy the [`NuGet.Config`](NuGet.Config) file from the root folder.

### 4. Setup Docker file

Copy the `Dockerfile` file from this repository to `/FrameworkBenchmarks/frameworks/CSharp/aspnetcore/aspcore-starcounter.dockerfile`.

Update the `aspcore-starcounter.dockerfile` file, `# Copy source files` section with the following:

```
COPY ./Starcounter.Techempower/NuGet.Config ./app/NuGet.Config
COPY ./Starcounter.Techempower/src/Starcounter.Techempower.csproj ./app/Starcounter.Techempower.csproj
COPY ./Starcounter.Techempower/src/DefaultRandom.cs ./app/DefaultRandom.cs
COPY ./Starcounter.Techempower/src/Fortune.cs ./app/Fortune.cs
COPY ./Starcounter.Techempower/src/FortuneOrm.cs ./app/FortuneOrm.cs
COPY ./Starcounter.Techempower/src/IFortune.cs ./app/IFortune.cs
COPY ./Starcounter.Techempower/src/IRandom.cs ./app/IRandom.cs
COPY ./Starcounter.Techempower/src/IWorld.cs ./app/IWorld.cs
COPY ./Starcounter.Techempower/src/Startup.cs ./app/Startup.cs
COPY ./Starcounter.Techempower/src/World.cs ./app/World.cs
COPY ./Starcounter.Techempower/src/WorldOrm.cs ./app/WorldOrm.cs
```

### 5. Run the benchmark

Execute the following command to verify that `Starcounter.Techempower` works:

```
sudo ./tfb --mode verify --test aspcore-starcounter
```

Execute the following command to benchmark `Starcounter.Techempower`:

```
sudo ./tfb --mode benchmark --test aspcore-starcounter
```

Execute the following command to benchmark Starcounter & ASP.NET Core with PostgreSQL:

```
sudo ./tfb --mode benchmark --test aspcore-ado-pg aspcore-ado-pg-up aspcore-starcounter
```

## Results

Hardware:

- OS: Ubuntu 18.04 under Hyper-V.
- CPU: 16 CPU Virtual Cores of Threadripper 1950x @ 4.0 GHz.
- RAM: 8 GB DDR4-3000 ECC CL18.
- SSD: Samsung 960 Pro 512 GB.

*Runs*:
