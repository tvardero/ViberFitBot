CREATE PROCEDURE SelectTop5
	@imei varchar(50),
	@sortByType smallint
AS BEGIN
	
	-- by ID
	IF (@sortByType = 0) 
		SELECT TOP 5 * FROM Tracks
		WHERE Imei = @imei
		ORDER BY Id

	-- by StartTimeUtc
	ELSE IF (@sortByType = 1)
		SELECT TOP 5 * FROM Tracks
		WHERE Imei = @imei
		ORDER BY StartTimeUtc

	ELSE IF (@sortByType = 2)
		SELECT TOP 5 * FROM Tracks
		WHERE Imei = @imei
		ORDER BY Duration

	ELSE 
		SELECT TOP 5 * FROM Tracks		
		WHERE Imei = @imei
		ORDER BY DistanceMetres;

END