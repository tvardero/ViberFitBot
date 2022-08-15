CREATE PROCEDURE AddNewTrackLocation
	@imei varchar(50),
	@latitude decimal(12,9),
	@longitude decimal(12,9),
	@dataSource int
AS BEGIN
	DECLARE 
		@currentTime datetime = GETUTCDATE(),
		@addedTrackId int;

	INSERT INTO [dbo].[TrackLocation] 
	VALUES (@imei, @latitude, @longitude, @currentTime, @currentTime, @dataSource);
	SET @addedTrackId = IDENT_CURRENT('TrackLocation');

	DECLARE
		@trackId uniqueidentifier,
		@trackStartUtc datetime,
		@prevDataTime datetime,
		@prevLatitude float,
		@prevLongitude float,
		@distance float;
	
	SELECT TOP 1 @trackId = t.Id, @trackStartUtc = f.date_track, @prevDataTime = l.date_track,  @prevLatitude = l.latitude, @prevLongitude = l.longitude, @distance = t.DistanceMetres FROM Tracks as t
	JOIN TrackLocation AS f
	ON f.Id = t.FirstDataId
	JOIN TrackLocation AS l
	ON l.Id = t.LatestDataId
	WHERE t.Imei = @imei
	ORDER BY f.date_track DESC;
	
	-- If null - create new track
	IF (@trackId IS NULL) 
		INSERT INTO Tracks(Id, Imei, StartTimeUtc, DistanceMetres, Duration, FirstDataId, LatestDataId) VALUES
		(NEWID(), @imei, @currentTime, 0, 0, @addedTrackId, @addedTrackId);

	-- If not null but > 30 min - create new track + delete if prev track has empty data
	ELSE IF (DATEDIFF(MINUTE, @currentTime, @prevDataTime) > 30) BEGIN
		IF (@distance = 0) 
			DELETE FROM Tracks
			WHERE Id = @trackId;

		INSERT INTO Tracks(Id, Imei, StartTimeUtc, DistanceMetres, Duration, FirstDataId, LatestDataId) VALUES
		(NEWID(), @imei, @currentTime, 0, 0, @addedTrackId, @addedTrackId);
		END

	-- If not null and < 30 min - update prev track
	ELSE BEGIN
		DECLARE
			@d float,
			@t1 bigint,
			@t2 bigint;
		EXEC CalculateDistanceBetweenPoints @prevLatitude, @prevLongitude, @latitude, @longitude, @d OUTPUT;
		
		SET @t1 = DATEDIFF_BIG( microsecond, '00010101', @currentTime ) * 10 +
           ( DATEPART( NANOSECOND, @currentTime ) % 1000 ) / 100;

		SET @t2 = DATEDIFF_BIG( microsecond, '00010101', @trackStartUtc ) * 10 +
           ( DATEPART( NANOSECOND, @trackStartUtc ) % 1000 ) / 100;

		UPDATE Tracks SET 
		DistanceMetres = DistanceMetres + @d,
		Duration = @t2 - @t1,
		LatestDataId = @addedTrackId
		WHERE Id = @trackId;
	END

END