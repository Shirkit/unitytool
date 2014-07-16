<?php
$results = fopen("results.txt", "a+");

$name = $_POST["results_name"];
$data = $_POST["results_data"];

fwrite($results, $name);
fwrite($results, $data);

fclose($results);

?>