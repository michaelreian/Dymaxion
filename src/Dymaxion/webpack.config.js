const path = require('path');
var debug = process.env.ASPNETCORE_ENVIRONMENT !== "Production";
var webpack = require('webpack');
const ExtractTextPlugin = require("extract-text-webpack-plugin");
const CheckerPlugin = require('awesome-typescript-loader').CheckerPlugin;
const bundleOutputDir = './wwwroot/app';

var extractTextPlugin = new ExtractTextPlugin({
    filename: 'picaroon.css'
});

module.exports = (env) => {
    const isDevBuild = !(env && env.prod);
    return [{
        stats: {
            modules: false
        },
        entry: {
            'picaroon': './app/picaroon/boot.ts'
        },
        resolve: { extensions: ['.js', '.ts'] },
        output: {
            path: path.join(__dirname, bundleOutputDir),
            filename: '[name].js',
            publicPath: '/app/'
        },
        module: {
            rules: [
                {
                    test: /\.ts$/,
                    include: /app/,
                    use: 'awesome-typescript-loader?silent=true'
                },
                {
                    test: /\.html$/,
                    use: 'raw-loader'
                },
                {
                    test: /\.css$/,
                    use: isDevBuild ? ['style-loader', 'css-loader'] : ExtractTextPlugin.extract({ use: 'css-loader?minimize' })
                },
                {
                    test: /\.(png|jpg|jpeg|gif|svg)$/,
                    use: 'url-loader?limit=25000'
                },
                {
                    test: /\.ttf(\?v=\d+\.\d+\.\d+)?$/,
                    loader: "url-loader?limit=10000&mimetype=application/octet-stream"
                },
                {
                    test: /\.eot(\?v=\d+\.\d+\.\d+)?$/,
                    loader: "file-loader"
                },
                {
                    test: /\.(woff|woff2)$/,
                    loader: "url-loader?prefix=font/&limit=5000"
                }
            ] 
        },
        plugins: [
            new CheckerPlugin(),
            new webpack.DllReferencePlugin({
                context: __dirname,
                manifest: require('./wwwroot/app/vendor-manifest.json')
            })
        ].concat(isDevBuild ? [
            // Plugins that apply in development builds only
            new webpack.SourceMapDevToolPlugin({
                filename: '[file].map', // Remove this line if you prefer inline source maps
                moduleFilenameTemplate: path.relative(bundleOutputDir, '[resourcePath]') // Point sourcemap entries to the original file locations on disk
            })
        ] : [
                // Plugins that apply in production builds only
                new webpack.optimize.UglifyJsPlugin(),
                new ExtractTextPlugin('css/site.css')
            ])
    }];
};

