module.exports = {
  "entry":{
    "bundle": "./app/js/boot.ts"
  },
  "output": {        
      "path": __dirname + '/wwwroot/app',
      "filename": '[name].js'
  },
  "resolve": {
    "extensions": ['.ts', '.webpack.js', '.web.js', '.js']
  },
  "devtool": 'source-map',
  "module": {
    "loaders": [
      {
        "test": /\.ts$/,
        "loader": 'awesome-typescript-loader'
      }
    ]
  }
};