const fs=require('fs');
const path=require('path');

var gulp = require('gulp');
var concat=require('gulp-concat');
var uglify = require('gulp-uglify');
var sourcemaps=require('gulp-sourcemaps');
var replace = require('gulp-replace');

gulp.task('combineandminifyjs',function(){
	try
	{
		var ptn = /\brequires\b.{1,}\[\n{0,}\t{0,}\s{0,}(\'|\")\w{1,}\.\w{1,}\.\w{1,}(\'|\"),\n{0,}\t{0,}\s{0,}(\'|\")\w{1,}\.\w{1,}\.\w{1,}(\'|\")\n{0,}\t{0,}\s{0,}\],/g;
		var abspath=process.argv[2].split('--')[1];
		var component=process.argv[3].split('--')[1];
		var activityilbo=process.argv[4].split('--')[1];	
		var langId=process.argv[5].split('--')[1];
		langId = langId && langId != '1' ? "_"+langId : "";
		var inputpath=path.relative('./',abspath);
		var inp=inputpath.split('\\');
		var ing="";	
		for(i=0;i<inp.length;i++)
		{
			ing=ing+inp[i]+'\/';
		}
		console.log(abspath,ing);
		gulp.src([ing+'Sources/'+component+'/view/'+activityilbo+'View'+langId+'.js',ing+'Sources/'+component+'/viewmodel/'+activityilbo+'ViewModel.js',ing+'Sources/'+component+'/controller/'+activityilbo+'Controller.js'])
			.pipe(concat(activityilbo+'View'+langId+'.js'))
			.pipe(replace(ptn, ''))
			.pipe(sourcemaps.init())
			.pipe(uglify())
			.pipe(sourcemaps.write('../../_sourcemap/'+component))
			.pipe(gulp.dest(ing+'Deliverables/'+component+'/view'));
		
	}
	catch(ex)
	{
		throw(ex);
	}

});
gulp.task('offlineSQL',function(){
	try
	{
		var ptn = /\brequires\b.{1,}\[\n{0,}\t{0,}\s{0,}(\'|\")\w{1,}\.\w{1,}\.\w{1,}(\'|\"),\n{0,}\t{0,}\s{0,}(\'|\")\w{1,}\.\w{1,}\.\w{1,}(\'|\")\n{0,}\t{0,}\s{0,}\],/g;
		var abspath=process.argv[2].split('--')[1];
		var component=process.argv[3].split('--')[1];
		var activityilbo=process.argv[4].split('--')[1];	
		var langId=process.argv[5].split('--')[1];
		langId = langId && langId != '1' ? "_"+langId : "";
		var inputpath=path.relative('./',abspath);
		var inp=inputpath.split('\\');
		var ing="";	
		for(i=0;i<inp.length;i++)
		{
			ing=ing+inp[i]+'\/';
		}
		console.log(ing+'Deliverables/'+component+'/scripts');
	gulp.src(ing+'Sources/'+component+'/scripts/'+activityilbo+'.sql')
			.pipe(gulp.dest(ing+'Deliverables/'+component+'/scripts'));
	}
	catch(ex)
	{
		throw(ex);
		console.log('SQL not found');
	}

});

gulp.task('default',['combineandminifyjs','offlineSQL']);
