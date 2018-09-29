var path = require("path");

function resolve(filePath) {
    return path.join(__dirname, filePath)
}

module.exports = {
    mode: "development",
    entry: "./src/Client/Client.fsproj",
    output: {
        path: path.join(__dirname, "./public"),
        filename: "bundle.js",
    },
    devServer: {
        contentBase: "./public",
        port: 8080,
    },
    resolve: {
        symlinks: false,
        modules: [resolve("node_modules/")]
    },
    module: {
        rules: [{
            test: /\.fs(x|proj)?$/,
            use: "fable-loader"
        }]
    }
}