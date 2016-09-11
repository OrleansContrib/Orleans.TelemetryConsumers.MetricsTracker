/* globals __dirname process */
"use strict";
var webpack = require('webpack');
var path = require('path');

var Promise = require('es6-promise').Promise;

module.exports = {
    entry: "./index.js",
    output: {
        filename: "bundle.js"
    },
    resolve: {
        extensions: ['', '.jsx', '.js'],
        modulesDirectories: ['node_modules'],
        fallback: path.join(__dirname, 'node_modules')
    },
    resolveLoader: { fallback: 'node_modules'},
    module: {
        loaders: [
            {
                test: /\.jsx?$/,
                loader: "babel-loader",
                exclude: /node_modules/,
                query: {
                    presets: ["react", "es2015", "stage-0"]
                }
            },
            { test: /jquery/, loader: 'expose?$!expose?jQuery' },
                  { test: /\.woff2?$/, loader: 'url-loader?limit=10000&mimetype=application/font-woff' },
      { test: /\.otf$/, loader: 'file-loader' },
      { test: /\.ttf$/, loader: 'file-loader' },
      { test: /\.eot$/, loader: 'file-loader' },
      { test: /\.svg$/, loader: 'file-loader' },
      { test: /\.html$/, loader: 'html' },
      { test: /\.css$/, loader: 'style-loader!css-loader' }
        ]
    }
};