BEGIN TRAN;

DECLARE @i int;

SELECT * 
INTO #tempTable
FROM TrackLocation;

SELECT @i = COUNT(*) FROM TrackLocation;

DECLARE 
	@id int,
	@imei varchar(50),
	@lat decimal(12,9),
	@lon decimal(12,9),
	@time datetime,
	@trackId uniqueidentifier,
	@start datetime,
	@prevLat decimal(12,9),
	@prevLon decimal(12, 9),
	@distance float;

WHILE @i > 0 BEGIN
	SELECT TOP 1
		@id =  id,
		@imei = IMEI,
		@lat =  latitude,
		@lon = longitude,
		@time = date_track
	FROM #tempTable
	ORDER BY date_track;

	SELECT TOP 1 
		@trackId = t.Id,
		@start = s.date_track,
		@prevLat = l.latitude,
		@prevLon = l.longitude,
		@distance = t.DistanceMetres
	FROM Tracks AS t
	JOIN TrackLocation AS s
	ON s.id = t.FirstDataId
	JOIN TrackLocation AS l
	ON l.id = t.LatestDataId
	WHERE t.Imei = @imei
	ORDER BY s.date_track DESC;
	
	-- If null - create new track
	IF (ISNULL(@distance, -1) = -1)
		INSERT INTO Tracks VALUES
		(NEWID(), @imei, @time, 0, 0, @id, @id);

	-- If not null but > 30 min - create new track + delete if prev track has empty data
	ELSE IF (DATEDIFF(MINUTE, @time, @start) > 30) BEGIN
		IF (@distance = 0) 
			DELETE FROM Tracks
			WHERE Id = @trackId;

		INSERT INTO Tracks VALUES
		(NEWID(), @imei, @time, 0, 0, @id, @id);
	END

	-- If not null and < 30 min - update prev track
	ELSE BEGIN
		DECLARE
			@d float = 0,
			@t1 bigint,
			@t2 bigint;

		EXEC CalculateDistanceBetweenPoints @lat, @lon, @prevLat, @prevLon, @d OUTPUT 
		
		SET @t1 = DATEDIFF_BIG( microsecond, '00010101', @time ) * 10 +
           ( DATEPART( NANOSECOND, @time ) % 1000 ) / 100;

		SET @t2 = DATEDIFF_BIG( microsecond, '00010101', @start ) * 10 +
           ( DATEPART( NANOSECOND, @start ) % 1000 ) / 100;

		UPDATE Tracks SET 
		DistanceMetres = DistanceMetres + @d,
		Duration = @t1 - @t2,
		LatestDataId = @id
		WHERE Id = @trackId;
	END
		

	DELETE FROM #tempTable
	WHERE id = @id;
	SET @i = @i - 1;
END;

DROP TABLE #tempTable;

COMMIT;
