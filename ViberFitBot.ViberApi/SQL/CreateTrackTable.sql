CREATE TABLE Tracks
(
	Id uniqueidentifier PRIMARY KEY,
	Imei varchar(50) NOT NULL,
	StartTimeUtc datetime2 NOT NULL,
	Duration bigint NOT NULL,
	DistanceMetres float NOT NULL,
	FirstDataId int REFERENCES [dbo].[TrackLocation](Id) ON DELETE NO ACTION NOT NULL,
	LatestDataId int REFERENCES [dbo].[TrackLocation](Id) ON DELETE NO ACTION NOT NULL
);